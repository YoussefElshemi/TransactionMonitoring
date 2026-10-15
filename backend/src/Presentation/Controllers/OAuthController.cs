using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Core.Enums;
using Core.Helpers;
using Core.Interfaces.Services;
using Core.Models.ApiClients;
using Core.ValueObjects;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Presentation.Attributes;
using Presentation.Constants;
using Presentation.Extensions;
using Presentation.Models.OAuth;

namespace Presentation.Controllers;

[SandboxMeterExempt]
[ApiController]
[AllowAnonymous]
[Route("oauth")]
public class OAuthController(
    IApiClientService apiClientService,
    IOptionsMonitor<BearerTokenOptions> bearerTokenOptions,
    TimeProvider timeProvider) : Controller
{
    [IdempotencyExempt(
        "An anonymous endpoint has no organisation, and an idempotency record is scoped to one. The caller acquires the identity this would be keyed against by calling it.")]
    [HttpPost("token")]
    [Consumes("application/x-www-form-urlencoded", "application/json")]
    [ProducesResponseType<TokenResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<OAuthErrorResponseDto>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<OAuthErrorResponseDto>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Token([FromForm] TokenFormRequestDto tokenFormRequestDto)
    {
        if (Request.HasFormContentType)
        {
            return await TokenAsync(tokenFormRequestDto.ToRequest());
        }

        TokenRequestDto tokenRequestDto;

        try
        {
            tokenRequestDto = await Request.ReadFromJsonAsync<TokenRequestDto>(HttpContext.RequestAborted) ?? new TokenRequestDto();
        }
        catch (JsonException)
        {
            return Error(StatusCodes.Status400BadRequest, OAuthConstants.InvalidRequest, "The request body could not be read as JSON.");
        }

        return await TokenAsync(tokenRequestDto);
    }

    private Task<IActionResult> TokenAsync(TokenRequestDto tokenRequestDto)
    {
        return tokenRequestDto.GrantType switch
        {
            OAuthConstants.ClientCredentialsGrantType => ClientCredentialsAsync(tokenRequestDto),
            OAuthConstants.RefreshTokenGrantType => RefreshTokenAsync(tokenRequestDto),
            _ => Task.FromResult(Error(StatusCodes.Status400BadRequest, OAuthConstants.UnsupportedGrantType,
                $"Supported grant types are '{OAuthConstants.ClientCredentialsGrantType}' and '{OAuthConstants.RefreshTokenGrantType}'."))
        };
    }

    private async Task<IActionResult> ClientCredentialsAsync(TokenRequestDto tokenRequestDto)
    {
        var (clientId, clientSecret) = ReadBasicCredentials() ?? (tokenRequestDto.ClientId, tokenRequestDto.ClientSecret);

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            return Error(StatusCodes.Status400BadRequest, OAuthConstants.InvalidRequest,
                "client_id and client_secret are required for the client_credentials grant.");
        }

        if (!TryParseScopes(tokenRequestDto.Scope, out var requestedScopes))
        {
            return Error(StatusCodes.Status400BadRequest, OAuthConstants.InvalidScope, "One or more requested scopes are not recognised.");
        }

        var authenticateApiClientRequest = new AuthenticateApiClientRequest
        {
            ClientId = new ClientId(clientId),
            ClientSecret = new ClientSecret(clientSecret),
            Scopes = requestedScopes
        };

        var result = await apiClientService.AuthenticateAsync(authenticateApiClientRequest, HttpContext.RequestAborted);

        return result.Outcome switch
        {
            ApiClientAuthenticationOutcome.Success => IssueToken(
                result.AuthenticatedApiClient!.ApiClientId,
                result.AuthenticatedApiClient.ApiClientSecretId,
                result.AuthenticatedApiClient.ClientId,
                result.AuthenticatedApiClient.OrganizationId,
                result.AuthenticatedApiClient.Scopes,
                result.AuthenticatedApiClient.Mode,
                result.AuthenticatedApiClient.ApiVersion),

            ApiClientAuthenticationOutcome.InvalidScope => Error(StatusCodes.Status400BadRequest,
                OAuthConstants.InvalidScope, "One or more requested scopes have not been granted to this client."),

            _ => Error(StatusCodes.Status401Unauthorized, OAuthConstants.InvalidClient, "Client authentication failed.")
        };
    }

    private async Task<IActionResult> RefreshTokenAsync(TokenRequestDto tokenRequestDto)
    {
        if (string.IsNullOrWhiteSpace(tokenRequestDto.RefreshToken))
        {
            return Error(StatusCodes.Status400BadRequest, OAuthConstants.InvalidRequest,
                "refresh_token is required for the refresh_token grant.");
        }

        var refreshTicket = Options.RefreshTokenProtector.Unprotect(tokenRequestDto.RefreshToken);

        if (refreshTicket?.Properties.ExpiresUtc is not { } expiresUtc ||
            timeProvider.GetUtcNow() >= expiresUtc ||
            refreshTicket.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value is not { } value ||
            !Guid.TryParse(value, out var apiClientId) ||
            refreshTicket.Principal.FindFirst(AuthConstants.SecretIdClaim)?.Value is not { } secretValue ||
            !Guid.TryParse(secretValue, out var apiClientSecretId))
        {
            return Error(StatusCodes.Status400BadRequest, OAuthConstants.InvalidGrant, "The refresh token is invalid or has expired.");
        }

        if (!await apiClientService.IsActiveAsync(new ApiClientId(apiClientId), HttpContext.RequestAborted))
        {
            return Error(StatusCodes.Status401Unauthorized, OAuthConstants.InvalidClient, "Client authentication failed.");
        }

        if (!await apiClientService.IsSecretActiveAsync(new ApiClientSecretId(apiClientSecretId), HttpContext.RequestAborted))
        {
            return Error(StatusCodes.Status401Unauthorized, OAuthConstants.InvalidClient, "Client authentication failed.");
        }

        var apiClient = await apiClientService.GetAsync(new ApiClientId(apiClientId), HttpContext.RequestAborted);

        var apiScopes = ReadScopes(refreshTicket.Principal).Intersect(apiClient.Scopes).ToList();

        if (apiScopes.Count == 0)
        {
            return Error(StatusCodes.Status400BadRequest, OAuthConstants.InvalidScope,
                "None of the scopes this token was granted are still granted to the client.");
        }

        // From the client, not the refresh token, so a client that changed mode can't mint tokens for its old mode.
        return IssueToken(apiClient.ApiClientId, new ApiClientSecretId(apiClientSecretId), apiClient.ClientId, apiClient.OrganizationId, apiScopes, apiClient.Mode, apiClient.ApiVersion);
    }

    private static List<ApiScope> ReadScopes(ClaimsPrincipal claimsPrincipal)
    {
        var apiScopes = new List<ApiScope>();

        foreach (var claim in claimsPrincipal.FindAll(AuthConstants.ScopeClaim))
        {
            if (ApiScopeHelper.TryParse(claim.Value, out var apiScope))
            {
                apiScopes.Add(apiScope);
            }
        }

        return apiScopes;
    }

    private BearerTokenOptions Options => bearerTokenOptions.Get(AuthConstants.ApiClientScheme);

    private IActionResult IssueToken(Guid apiClientId, Guid apiClientSecretId, string clientId, Guid organizationId, List<ApiScope> apiScopes, Mode mode, string apiVersion)
    {
        var options = Options;
        var utcNow = timeProvider.GetUtcNow();
        var principal = BuildPrincipal(apiClientId, apiClientSecretId, clientId, organizationId, apiScopes, mode, apiVersion);

        var accessTicket = new AuthenticationTicket(
            principal,
            new AuthenticationProperties { ExpiresUtc = utcNow + options.BearerTokenExpiration },
            $"{AuthConstants.ApiClientScheme}:AccessToken");

        var refreshTicket = new AuthenticationTicket(
            principal,
            new AuthenticationProperties { ExpiresUtc = utcNow + options.RefreshTokenExpiration },
            $"{AuthConstants.ApiClientScheme}:RefreshToken");

        return Ok(new TokenResponseDto
        {
            AccessToken = options.BearerTokenProtector.Protect(accessTicket),
            TokenType = "Bearer",
            ExpiresIn = (long)options.BearerTokenExpiration.TotalSeconds,
            RefreshToken = options.RefreshTokenProtector.Protect(refreshTicket),
            Scope = string.Join(' ', apiScopes.Select(ApiScopeHelper.ToScopeString))
        });
    }

    private static ClaimsPrincipal BuildPrincipal(Guid apiClientId, Guid apiClientSecretId, string clientId, Guid organizationId, IEnumerable<ApiScope> apiScopes, Mode mode, string apiVersion)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, apiClientId.ToString()),
            new(AuthConstants.SecretIdClaim, apiClientSecretId.ToString()),
            new(AuthConstants.ClientIdClaim, clientId),
            new(AuthConstants.OrganizationIdClaim, organizationId.ToString()),
            new(ModeConstants.ModeClaim, ModeHeaderHelper.ToHeaderValue(mode)),

            // Carried in the token, so a moved pin takes effect on the client's next token.
            new(AuthConstants.ApiVersionClaim, apiVersion)
        };

        claims.AddRange(apiScopes.Select(x => new Claim(AuthConstants.ScopeClaim, ApiScopeHelper.ToScopeString(x))));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, AuthConstants.ApiClientScheme));
    }

    private (string ClientId, string ClientSecret)? ReadBasicCredentials()
    {
        if (!AuthenticationHeaderValue.TryParse(Request.Headers.Authorization, out var header) ||
            !string.Equals(header.Scheme, "Basic", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(header.Parameter))
        {
            return null;
        }

        string decoded;

        try
        {
            decoded = Encoding.UTF8.GetString(Convert.FromBase64String(header.Parameter));
        }
        catch (FormatException)
        {
            return null;
        }

        var separator = decoded.IndexOf(':');

        return separator < 0 ? null : (decoded[..separator], decoded[(separator + 1)..]);
    }

    private static bool TryParseScopes(string? scope, out List<ApiScope>? apiScopes)
    {
        apiScopes = null;

        if (string.IsNullOrWhiteSpace(scope))
        {
            return true;
        }

        var parsed = new List<ApiScope>();

        foreach (var value in scope.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!ApiScopeHelper.TryParse(value, out var apiScope))
            {
                return false;
            }

            parsed.Add(apiScope);
        }

        apiScopes = parsed;

        return true;
    }

    private IActionResult Error(int statusCode, string error, string errorDescription)
    {
        return StatusCode(statusCode, new OAuthErrorResponseDto
        {
            Error = error,
            ErrorDescription = errorDescription
        });
    }
}
