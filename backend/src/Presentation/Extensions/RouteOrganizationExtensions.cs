using Core.ValueObjects;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Extensions;

public static class RouteOrganizationExtensions
{
    private const string OrganizationIdRouteKey = "OrganizationId";

    public static bool TryGetRouteOrganizationId(this ActionContext context, out OrganizationId organizationId)
    {
        var routeValue = context.RouteData.Values
            .FirstOrDefault(x => string.Equals(x.Key, OrganizationIdRouteKey, StringComparison.OrdinalIgnoreCase))
            .Value?
            .ToString();

        if (!Guid.TryParse(routeValue, out var parsed))
        {
            organizationId = default;

            return false;
        }

        organizationId = new OrganizationId(parsed);

        return true;
    }
}
