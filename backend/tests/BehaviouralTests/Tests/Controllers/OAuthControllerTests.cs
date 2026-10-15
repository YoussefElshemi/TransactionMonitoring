using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using AutoFixture;
using BehaviouralTests.TestHelpers;
using Core.Enums;
using Core.Helpers;
using Core.Interfaces.Services;
using Core.Models.ApiClients;
using Core.ValueObjects;
using FluentAssertions;
using Infrastructure;
using Infrastructure.Configurations;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Constants;
using Presentation.Models.OAuth;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

namespace BehaviouralTests.Tests.Controllers;

public class OAuthControllerTests(TestFixture testFixture) : IClassFixture<TestFixture>, IAsyncLifetime
{
    private readonly Fixture _fixture = AutoFixtureHelper.Create();
    private readonly HttpClient _httpClient = testFixture.CreateClient();

    private const string Email = "username@mail.com";
    private const string Password = "P@$$w0rd";
    private static readonly Guid OrganizationId = new("4d6389e9-f5d1-4325-a004-915865e3adec");

    [Fact]
    public async Task Token_GivenValidClientCredentials_ShouldReturnAnRfcShapedAccessToken()
    {
        // Arrange
        var created = await CreateApiClientAsync();

        // Act
        var response = await PostFormAsync(ClientCredentialsForm(created));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().ContainKey(HeaderNames.CorrelationId);

        var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponseDto>();
        tokenResponse.Should().NotBeNull();
        tokenResponse!.TokenType.Should().Be("Bearer");
        tokenResponse.AccessToken.Should().NotBeNullOrWhiteSpace();
        tokenResponse.RefreshToken.Should().NotBeNullOrWhiteSpace();
        tokenResponse.ExpiresIn.Should().Be(3600);
        tokenResponse.Scope.Should().Be("rules:evaluate rulesets:evaluate");

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("\"access_token\"");
        body.Should().Contain("\"token_type\"");
        body.Should().Contain("\"expires_in\"");
    }

    [Fact]
    public async Task Token_GivenCredentialsInTheAuthorizationHeader_ShouldReturnAnAccessToken()
    {
        // Arrange
        var created = await CreateApiClientAsync();

        var request = new HttpRequestMessage(HttpMethod.Post, "/oauth/token")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = OAuthConstants.ClientCredentialsGrantType
            })
        };

        var credentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{created.ApiClient.ClientId}:{created.ClientSecret}"));

        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

        // Act
        var response = await _httpClient.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponseDto>();
        tokenResponse!.AccessToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Token_GivenAJsonBody_ShouldAlsoBeAccepted()
    {
        // Arrange
        var created = await CreateApiClientAsync();

        // Act
        var response = await _httpClient.PostAsJsonAsync("/oauth/token", new TokenRequestDto
        {
            GrantType = OAuthConstants.ClientCredentialsGrantType,
            ClientId = created.ApiClient.ClientId,
            ClientSecret = created.ClientSecret
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponseDto>();
        tokenResponse!.AccessToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Token_GivenValidClientCredentials_ShouldStampTheSecretAsUsed()
    {
        // Arrange
        var created = await CreateApiClientAsync();

        // Act
        await PostFormAsync(ClientCredentialsForm(created));

        // Assert
        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        var secret = await context.ApiClientSecrets.FirstAsync(x => x.ApiClientId == created.ApiClient.ApiClientId);
        secret.LastUsedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Token_GivenTheWrongSecret_ShouldReturnInvalidClient()
    {
        // Arrange
        var created = await CreateApiClientAsync();

        var form = ClientCredentialsForm(created);
        form["client_secret"] = ApiClientCredentialHelper.GenerateClientSecret(Mode.Live);

        // Act
        var response = await PostFormAsync(form);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var error = await response.Content.ReadFromJsonAsync<OAuthErrorResponseDto>();
        error!.Error.Should().Be(OAuthConstants.InvalidClient);
    }

    [Fact]
    public async Task Token_GivenAnUnknownClientId_ShouldReturnTheSameErrorAsAWrongSecret()
    {
        // Act
        var response = await PostFormAsync(new Dictionary<string, string>
        {
            ["grant_type"] = OAuthConstants.ClientCredentialsGrantType,
            ["client_id"] = ApiClientCredentialHelper.GenerateClientId(Mode.Live),
            ["client_secret"] = ApiClientCredentialHelper.GenerateClientSecret(Mode.Live)
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var error = await response.Content.ReadFromJsonAsync<OAuthErrorResponseDto>();
        error!.Error.Should().Be(OAuthConstants.InvalidClient);
    }

    [Fact]
    public async Task Token_ClientIsDisabled_ShouldReturnInvalidClient()
    {
        // Arrange
        var created = await CreateApiClientAsync();
        await SetClientEnabledAsync(created.ApiClient.ApiClientId, false);

        // Act
        var response = await PostFormAsync(ClientCredentialsForm(created));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Token_SecretIsRevoked_ShouldReturnInvalidClient()
    {
        // Arrange
        var created = await CreateApiClientAsync();

        using (var scope = testFixture.Services.CreateScope())
        {
            var apiClientService = scope.ServiceProvider.GetRequiredService<IApiClientService>();
            var secrets = await apiClientService.ListSecretsAsync(created.ApiClient.ApiClientId, CancellationToken.None);
            await apiClientService.RevokeSecretAsync(secrets[0].ApiClientSecretId, CancellationToken.None);
        }

        // Act
        var response = await PostFormAsync(ClientCredentialsForm(created));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Token_RequestsAScopeThatWasNotGranted_ShouldReturnInvalidScope()
    {
        // Arrange
        var created = await CreateApiClientAsync([ApiScope.RulesEvaluate]);

        var form = ClientCredentialsForm(created);
        form["scope"] = ApiScopeHelper.ToScopeString(ApiScope.RuleSetsEvaluate);

        // Act
        var response = await PostFormAsync(form);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var error = await response.Content.ReadFromJsonAsync<OAuthErrorResponseDto>();
        error!.Error.Should().Be(OAuthConstants.InvalidScope);
    }

    [Fact]
    public async Task Token_RequestsASubsetOfGrantedScopes_ShouldNarrowTheToken()
    {
        // Arrange
        var created = await CreateApiClientAsync();

        var form = ClientCredentialsForm(created);
        form["scope"] = ApiScopeHelper.ToScopeString(ApiScope.RulesEvaluate);

        // Act
        var response = await PostFormAsync(form);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponseDto>();
        tokenResponse!.Scope.Should().Be("rules:evaluate");
    }

    [Fact]
    public async Task Token_RequestsAnUnrecognisedScope_ShouldReturnInvalidScope()
    {
        // Arrange
        var created = await CreateApiClientAsync();

        var form = ClientCredentialsForm(created);
        form["scope"] = "rules:delete";

        // Act
        var response = await PostFormAsync(form);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var error = await response.Content.ReadFromJsonAsync<OAuthErrorResponseDto>();
        error!.Error.Should().Be(OAuthConstants.InvalidScope);
    }

    [Fact]
    public async Task Token_GivenAnUnsupportedGrantType_ShouldReturnUnsupportedGrantType()
    {
        // Act
        var response = await PostFormAsync(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = _fixture.Create<string>(),
            ["client_secret"] = _fixture.Create<string>()
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var error = await response.Content.ReadFromJsonAsync<OAuthErrorResponseDto>();
        error!.Error.Should().Be(OAuthConstants.UnsupportedGrantType);
    }

    [Fact]
    public async Task Token_MissingCredentials_ShouldReturnInvalidRequest()
    {
        // Act
        var response = await PostFormAsync(new Dictionary<string, string>
        {
            ["grant_type"] = OAuthConstants.ClientCredentialsGrantType
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var error = await response.Content.ReadFromJsonAsync<OAuthErrorResponseDto>();
        error!.Error.Should().Be(OAuthConstants.InvalidRequest);
    }

    [Fact]
    public async Task Token_GivenAnInvalidRefreshToken_ShouldReturnInvalidGrant()
    {
        // Act
        var response = await PostFormAsync(new Dictionary<string, string>
        {
            ["grant_type"] = OAuthConstants.RefreshTokenGrantType,
            ["refresh_token"] = _fixture.Create<string>()
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var error = await response.Content.ReadFromJsonAsync<OAuthErrorResponseDto>();
        error!.Error.Should().Be(OAuthConstants.InvalidGrant);
    }

    [Fact]
    public async Task Token_GivenAValidRefreshToken_ShouldReturnANewAccessToken()
    {
        // Arrange
        var created = await CreateApiClientAsync();
        var tokenResponse = await (await PostFormAsync(ClientCredentialsForm(created))).Content.ReadFromJsonAsync<TokenResponseDto>();

        // Act
        var response = await PostFormAsync(new Dictionary<string, string>
        {
            ["grant_type"] = OAuthConstants.RefreshTokenGrantType,
            ["refresh_token"] = tokenResponse!.RefreshToken
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var refreshed = await response.Content.ReadFromJsonAsync<TokenResponseDto>();
        refreshed!.AccessToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Token_RefreshingAfterTheClientIsDisabled_ShouldReturnInvalidClient()
    {
        // Arrange
        var created = await CreateApiClientAsync();
        var tokenResponse = await (await PostFormAsync(ClientCredentialsForm(created))).Content.ReadFromJsonAsync<TokenResponseDto>();

        await SetClientEnabledAsync(created.ApiClient.ApiClientId, false);

        // Act
        var response = await PostFormAsync(new Dictionary<string, string>
        {
            ["grant_type"] = OAuthConstants.RefreshTokenGrantType,
            ["refresh_token"] = tokenResponse!.RefreshToken
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Token_RefreshingANarrowedToken_ShouldNotWidenTheScope()
    {
        // Arrange
        var created = await CreateApiClientAsync();

        var form = ClientCredentialsForm(created);
        form["scope"] = ApiScopeHelper.ToScopeString(ApiScope.RulesEvaluate);

        var tokenResponse = await (await PostFormAsync(form)).Content.ReadFromJsonAsync<TokenResponseDto>();
        tokenResponse!.Scope.Should().Be("rules:evaluate");

        // Act
        var response = await PostFormAsync(RefreshTokenForm(tokenResponse.RefreshToken));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var refreshed = await response.Content.ReadFromJsonAsync<TokenResponseDto>();
        refreshed!.Scope.Should().Be("rules:evaluate");
    }

    [Fact]
    public async Task Token_RefreshingAfterAScopeIsTakenAway_ShouldDropThatScope()
    {
        // Arrange
        var created = await CreateApiClientAsync();
        var tokenResponse = await (await PostFormAsync(ClientCredentialsForm(created))).Content.ReadFromJsonAsync<TokenResponseDto>();

        await SetClientScopesAsync(created.ApiClient.ApiClientId, [ApiScope.RulesEvaluate]);

        // Act
        var response = await PostFormAsync(RefreshTokenForm(tokenResponse!.RefreshToken));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var refreshed = await response.Content.ReadFromJsonAsync<TokenResponseDto>();
        refreshed!.Scope.Should().Be("rules:evaluate");
    }

    [Fact]
    public async Task Token_RefreshingAfterEveryGrantedScopeIsTakenAway_ShouldReturnInvalidScope()
    {
        // Arrange
        var created = await CreateApiClientAsync();

        var form = ClientCredentialsForm(created);
        form["scope"] = ApiScopeHelper.ToScopeString(ApiScope.RulesEvaluate);

        var tokenResponse = await (await PostFormAsync(form)).Content.ReadFromJsonAsync<TokenResponseDto>();

        await SetClientScopesAsync(created.ApiClient.ApiClientId, [ApiScope.RuleSetsEvaluate]);

        // Act
        var response = await PostFormAsync(RefreshTokenForm(tokenResponse!.RefreshToken));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var error = await response.Content.ReadFromJsonAsync<OAuthErrorResponseDto>();
        error!.Error.Should().Be(OAuthConstants.InvalidScope);
    }

    [Fact]
    public async Task Token_RefreshingAfterTheSecretIsRevoked_ShouldReturnInvalidClient()
    {
        // Arrange
        var created = await CreateApiClientAsync();
        var tokenResponse = await (await PostFormAsync(ClientCredentialsForm(created))).Content.ReadFromJsonAsync<TokenResponseDto>();

        await RevokeAllSecretsAsync(created.ApiClient.ApiClientId);

        // Act
        var response = await PostFormAsync(RefreshTokenForm(tokenResponse!.RefreshToken));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var error = await response.Content.ReadFromJsonAsync<OAuthErrorResponseDto>();
        error!.Error.Should().Be(OAuthConstants.InvalidClient);
    }

    [Fact]
    public async Task Token_RefreshingATokenFromAnotherSecret_ShouldSurviveTheFirstSecretBeingRevoked()
    {
        // Arrange
        var created = await CreateApiClientAsync();
        var rotated = await CreateSecretAsync(created.ApiClient.ApiClientId);

        var form = ClientCredentialsForm(created);
        form["client_secret"] = rotated.ClientSecret;

        var tokenResponse = await (await PostFormAsync(form)).Content.ReadFromJsonAsync<TokenResponseDto>();

        await RevokeSecretAsync(created.ApiClient.ApiClientId, x => x.ApiClientSecretId != rotated.ApiClientSecret.ApiClientSecretId);

        // Act
        var response = await PostFormAsync(RefreshTokenForm(tokenResponse!.RefreshToken));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Token_GivenAMalformedJsonBody_ShouldReturnInvalidRequest()
    {
        // Act
        var response = await _httpClient.PostAsync("/oauth/token",
            new StringContent("{ not json", Encoding.UTF8, "application/json"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var error = await response.Content.ReadFromJsonAsync<OAuthErrorResponseDto>();
        error!.Error.Should().Be(OAuthConstants.InvalidRequest);
    }

    [Fact]
    public async Task Token_RequestedRepeatedly_ShouldStampLastUsedWithoutWritingAuditRows()
    {
        // Arrange
        var created = await CreateApiClientAsync();

        // Act
        for (var i = 0; i < 3; i++)
        {
            (await PostFormAsync(ClientCredentialsForm(created))).StatusCode.Should().Be(HttpStatusCode.OK);
        }

        // Assert
        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        Guid apiClientId = created.ApiClient.ApiClientId;
        var secret = await context.ApiClientSecrets.FirstAsync(x => x.ApiClientId == apiClientId);
        secret.LastUsedAt.Should().NotBeNull();

        var auditLogs = await context.AuditLogs
            .Where(x => x.EntityTypeId == (int)AuditableEntityType.ApiClientSecret &&
                        x.EntityId == secret.ApiClientSecretId &&
                        x.OperationId == (int)AuditOperation.Updated)
            .CountAsync();

        auditLogs.Should().Be(0);
    }

    private static Dictionary<string, string> RefreshTokenForm(string refreshToken)
    {
        return new Dictionary<string, string>
        {
            ["grant_type"] = OAuthConstants.RefreshTokenGrantType,
            ["refresh_token"] = refreshToken
        };
    }

    private static Dictionary<string, string> ClientCredentialsForm(CreatedApiClient createdApiClient)
    {
        return new Dictionary<string, string>
        {
            ["grant_type"] = OAuthConstants.ClientCredentialsGrantType,
            ["client_id"] = createdApiClient.ApiClient.ClientId,
            ["client_secret"] = createdApiClient.ClientSecret
        };
    }

    private Task<HttpResponseMessage> PostFormAsync(Dictionary<string, string> form)
    {
        return AuthenticationHelper.PostTokenFormAsync(_httpClient, form);
    }

    private async Task<CreatedApiClient> CreateApiClientAsync(List<ApiScope>? scopes = null)
    {
        var user = await AuthenticationHelper.CreateUserAsync(
            testFixture.Services, Email, Password, [UserRole.User, UserRole.Manager], OrganizationId);

        return await AuthenticationHelper.CreateApiClientAsync(testFixture.Services, new CreateApiClientRequest
        {
            FriendlyName = new FriendlyName(_fixture.Create<string>()),
            UserId = new ApplicationUserId(user.Id),
            OrganizationId = new OrganizationId(OrganizationId),
            Scopes = scopes ?? [ApiScope.RulesEvaluate, ApiScope.RuleSetsEvaluate]
        });
    }

    private async Task SetClientEnabledAsync(ApiClientId apiClientId, bool isEnabled)
    {
        using var scope = testFixture.Services.CreateScope();
        var apiClientService = scope.ServiceProvider.GetRequiredService<IApiClientService>();

        await apiClientService.UpdateAsync(new UpdateApiClientRequest
        {
            ApiClientId = apiClientId,
            FriendlyName = null,
            IsEnabled = new IsEnabled(isEnabled),
            Scopes = null,
            ApiVersion = null
        }, CancellationToken.None);
    }

    private async Task SetClientScopesAsync(ApiClientId apiClientId, List<ApiScope> apiScopes)
    {
        using var scope = testFixture.Services.CreateScope();
        var apiClientService = scope.ServiceProvider.GetRequiredService<IApiClientService>();

        await apiClientService.UpdateAsync(new UpdateApiClientRequest
        {
            ApiClientId = apiClientId,
            FriendlyName = null,
            IsEnabled = null,
            Scopes = apiScopes,
            ApiVersion = null
        }, CancellationToken.None);
    }

    private async Task<CreatedApiClientSecret> CreateSecretAsync(ApiClientId apiClientId)
    {
        using var scope = testFixture.Services.CreateScope();
        var apiClientService = scope.ServiceProvider.GetRequiredService<IApiClientService>();

        var apiClient = await apiClientService.GetAsync(apiClientId, CancellationToken.None);

        return await apiClientService.CreateSecretAsync(new CreateApiClientSecretRequest
        {
            ApiClientId = apiClientId,
            OrganizationId = apiClient.OrganizationId,
            UserId = apiClient.CreatedByUserId,
            ExpiresAt = null
        }, CancellationToken.None);
    }

    private Task RevokeAllSecretsAsync(ApiClientId apiClientId)
    {
        return RevokeSecretAsync(apiClientId, _ => true);
    }

    private async Task RevokeSecretAsync(ApiClientId apiClientId, Func<ApiClientSecret, bool> predicate)
    {
        using var scope = testFixture.Services.CreateScope();
        var apiClientService = scope.ServiceProvider.GetRequiredService<IApiClientService>();

        var secrets = await apiClientService.ListSecretsAsync(apiClientId, CancellationToken.None);

        foreach (var secret in secrets.Where(predicate))
        {
            await apiClientService.RevokeSecretAsync(secret.ApiClientSecretId, CancellationToken.None);
        }
    }

    public async Task InitializeAsync()
    {
        await testFixture.ResetDatabaseAsync();

        var organizationEntities = new List<OrganizationEntity>
        {
            OrganizationConfigurations.DefaultOrganization,
            FakeOrganizationEntity.CreateValid(_fixture) with
            {
                OrganizationId = OrganizationId,
                StatusId = (int)OrganizationStatus.Enabled,
                IsDeleted = false
            }
        };

        await DatabaseHelper.CreateOrganizations(testFixture.Services, organizationEntities);
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}
