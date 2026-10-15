using Core.Enums;
using Core.Interfaces;
using Core.Interfaces.Services;
using Core.ValueObjects;
using Microsoft.AspNetCore.Mvc.Filters;
using Presentation.Attributes;
using Presentation.Extensions;

namespace Presentation.Filters;

public class SandboxWriteMeterFilter(
    ISandboxCreditService sandboxCreditService,
    IApplicationUserService applicationUserService,
    IModeContext modeContext) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (ShouldCharge(context))
        {
            await sandboxCreditService.EnforceAsync(
                await ResolveOrganizationAsync(context),
                BillableResource.SandboxWrite,
                new Resource(Describe(context)),
                new Count(1),
                context.HttpContext.RequestAborted);
        }

        await next();
    }

    private bool ShouldCharge(ActionExecutingContext context)
    {
        return modeContext.CurrentOrLive == Mode.Test &&
               IsMutating(context.HttpContext.Request.Method) &&
               !context.ActionDescriptor.EndpointMetadata.OfType<SandboxMeterExemptAttribute>().Any();
    }

    private static bool IsMutating(string method)
    {
        return HttpMethods.IsPost(method) ||
               HttpMethods.IsPut(method) ||
               HttpMethods.IsPatch(method) ||
               HttpMethods.IsDelete(method);
    }

    private async Task<OrganizationId> ResolveOrganizationAsync(ActionExecutingContext context)
    {
        if (context.HttpContext.User.IsApiClient())
        {
            return new OrganizationId(context.HttpContext.User.GetOrganizationId());
        }

        var authenticatedUser = await applicationUserService.GetAsync(
            context.HttpContext.User, context.HttpContext.RequestAborted);

        return authenticatedUser.OrganizationId;
    }

    private static string Describe(ActionExecutingContext context)
    {
        return context.ActionDescriptor.AttributeRouteInfo?.Template ?? context.HttpContext.Request.Path.ToString();
    }
}
