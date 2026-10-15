using Core.Enums;
using Core.Extensions;
using Core.Interfaces.Services;
using Core.Models.Users;
using Core.ValueObjects;
using Microsoft.AspNetCore.Mvc.Filters;
using Presentation.Extensions;

namespace Presentation.Attributes;

public class ValidateAuthenticatedUserAttribute : ActionFilterAttribute
{
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.HttpContext.User.IsApiClient())
        {
            var apiClientService = context.HttpContext.RequestServices.GetRequiredService<IApiClientService>();
            var apiClientId = new ApiClientId(context.HttpContext.User.GetApiClientId());

            var authenticatedApiClient = await apiClientService.GetAsync(apiClientId, context.HttpContext.RequestAborted);

            authenticatedApiClient.ThrowIfNotEnabled();
            authenticatedApiClient.ThrowIfOrganizationNotEnabled();

            await next();

            return;
        }

        var applicationUserService = context.HttpContext.RequestServices.GetRequiredService<IApplicationUserService>();
        var authenticatedUser = await applicationUserService.GetAsync(context.HttpContext.User, context.HttpContext.RequestAborted);

        if (!await MayActWithoutOrganizationAsync(context, authenticatedUser))
        {
            authenticatedUser.ThrowIfNotEnabled();
        }

        authenticatedUser.ThrowIfOrganizationNotEnabled();

        await next();
    }

    // Platform admins sit in the default organisation, so they need both the permission and an exempt endpoint.
    private static async Task<bool> MayActWithoutOrganizationAsync(ActionExecutingContext context, ApplicationUser authenticatedUser)
    {
        if (authenticatedUser.IsEnabled)
        {
            return false;
        }

        if (!ActsOutsideCallersOrganization(context))
        {
            return false;
        }

        var permissionService = context.HttpContext.RequestServices.GetRequiredService<IPermissionService>();

        var effectivePermissions = await permissionService.ResolveAsync(
            context.HttpContext.User, context.HttpContext.RequestAborted);

        return effectivePermissions.Holds(Permission.PlatformAdminister);
    }

    private static bool ActsOutsideCallersOrganization(ActionExecutingContext context)
    {
        return context.ActionDescriptor.EndpointMetadata.OfType<OrganizationScopeExemptAttribute>().Any() ||
               context.TryGetRouteOrganizationId(out _);
    }
}
