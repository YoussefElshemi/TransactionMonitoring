using System.Security.Claims;
using Core.Enums;
using Presentation.Constants;

namespace Presentation.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static bool IsApiClient(this ClaimsPrincipal claimsPrincipal)
    {
        return claimsPrincipal.FindApiClientIdentity() is not null;
    }

    public static bool HasApiScope(this ClaimsPrincipal claimsPrincipal, string scope)
    {
        return claimsPrincipal.FindApiClientIdentity()?.HasClaim(AuthConstants.ScopeClaim, scope) == true;
    }

    public static Guid GetApiClientId(this ClaimsPrincipal claimsPrincipal)
    {
        return GetGuidClaim(claimsPrincipal, ClaimTypes.NameIdentifier);
    }

    public static Guid GetOrganizationId(this ClaimsPrincipal claimsPrincipal)
    {
        return GetGuidClaim(claimsPrincipal, AuthConstants.OrganizationIdClaim);
    }

    // Tokens issued before the mode claim existed are all live keys.
    public static Mode GetMode(this ClaimsPrincipal claimsPrincipal)
    {
        var value = claimsPrincipal.FindApiClientIdentity()?.FindFirst(ModeConstants.ModeClaim)?.Value;

        return ModeHeaderHelper.TryParse(value, out var mode) ? mode : Mode.Live;
    }

    public static string? FindApiVersion(this ClaimsPrincipal claimsPrincipal)
    {
        return claimsPrincipal.FindApiClientIdentity()?.FindFirst(AuthConstants.ApiVersionClaim)?.Value;
    }

    // Read off the API client identity: the merged principal puts the user identity's NameIdentifier first.
    private static ClaimsIdentity? FindApiClientIdentity(this ClaimsPrincipal claimsPrincipal)
    {
        return claimsPrincipal.Identities.FirstOrDefault(x => x.HasClaim(y => y.Type == AuthConstants.ClientIdClaim));
    }

    private static Guid GetGuidClaim(ClaimsPrincipal claimsPrincipal, string claimType)
    {
        var value = claimsPrincipal.FindApiClientIdentity()?.FindFirst(claimType)?.Value;

        if (!Guid.TryParse(value, out var parsed))
        {
            throw new InvalidOperationException($"The principal does not carry a valid '{claimType}' claim.");
        }

        return parsed;
    }
}
