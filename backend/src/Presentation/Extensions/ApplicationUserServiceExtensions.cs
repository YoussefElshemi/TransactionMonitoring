using System.Security.Claims;
using Core.Enums;
using Core.Interfaces.Services;
using Core.Models.Users;
using Core.ValueObjects;
using Presentation.Factories;

namespace Presentation.Extensions;

public static class ApplicationUserServiceExtensions
{
    public static EnrichmentContext ToEnrichmentContext(this ApplicationUser authenticatedUser, ClaimsPrincipal claimsPrincipal)
    {
        return new EnrichmentContext
        {
            OrganizationId = authenticatedUser.OrganizationId,
            IsAdmin = claimsPrincipal.IsInRole(nameof(UserRole.Admin))
        };
    }

    public static async Task<EnrichmentContext> ResolveEnrichmentContextAsync(
        this IApplicationUserService applicationUserService,
        ClaimsPrincipal claimsPrincipal,
        CancellationToken stoppingToken)
    {
        var organizationId = await applicationUserService.ResolveOrganizationIdAsync(claimsPrincipal, stoppingToken);

        return new EnrichmentContext
        {
            OrganizationId = organizationId,
            IsAdmin = claimsPrincipal.IsInRole(nameof(UserRole.Admin))
        };
    }

    public static async Task<OrganizationId> ResolveOrganizationIdAsync(
        this IApplicationUserService applicationUserService,
        ClaimsPrincipal claimsPrincipal,
        CancellationToken stoppingToken)
    {
        if (claimsPrincipal.IsApiClient())
        {
            return new OrganizationId(claimsPrincipal.GetOrganizationId());
        }

        var authenticatedUser = await applicationUserService.GetAsync(claimsPrincipal, stoppingToken);

        return authenticatedUser.OrganizationId;
    }
}
