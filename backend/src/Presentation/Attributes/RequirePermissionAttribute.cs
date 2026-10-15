using System.Collections.Concurrent;
using System.Reflection;
using Core.Enums;
using Core.Interfaces.Services;
using Core.ValueObjects;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Presentation.Extensions;

namespace Presentation.Attributes;

// A filter, not a policy: permissions are resolved from the database per request. Checks the permission is held somewhere (arguments aren't bound yet) and that the route's organisation is the caller's own or they hold the platform permission.
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequirePermissionAttribute(Permission permission) : ActionFilterAttribute
{
    private const string CaseIdRouteKey = "CaseId";
    private const string RouteCaseIdRouteKey = "RouteCaseId";
    private const string QueueIdRouteKey = "QueueId";
    private const string RuleIdRouteKey = "RuleId";
    private const string RuleSetIdRouteKey = "RuleSetId";

    private static readonly ConcurrentDictionary<MethodInfo, Permission[]> ActionPermissions = new();

    public Permission Permission { get; } = permission;

    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // An action-level attribute replaces the controller's rather than combining with it, unlike MVC's default.
        if (IsSupersededByAction(context))
        {
            await next();

            return;
        }

        if (context.HttpContext.User.IsApiClient())
        {
            await next();

            return;
        }

        var permissionService = context.HttpContext.RequestServices.GetRequiredService<IPermissionService>();

        await permissionService.EnsureHeldAsync(context.HttpContext.User, Permission, context.HttpContext.RequestAborted);

        if (context.TryGetRouteOrganizationId(out var organizationId))
        {
            await permissionService.EnsureOrganizationReachableAsync(
                context.HttpContext.User, organizationId, context.HttpContext.RequestAborted);
        }

        await EnsureHeldOverRouteResourceAsync(context, permissionService);

        await next();
    }

    private async Task EnsureHeldOverRouteResourceAsync(ActionExecutingContext context, IPermissionService permissionService)
    {
        var stoppingToken = context.HttpContext.RequestAborted;
        var user = context.HttpContext.User;

        if (TryGetRouteGuid(context, RuleIdRouteKey, out var ruleId))
        {
            await permissionService.EnsureHeldOverRuleAsync(user, Permission, new RuleId(ruleId), stoppingToken);

            return;
        }

        if (TryGetRouteGuid(context, RuleSetIdRouteKey, out var ruleSetId))
        {
            await permissionService.EnsureHeldOverRuleSetAsync(user, Permission, new RuleSetId(ruleSetId), stoppingToken);

            return;
        }

        // Two spellings: the case-alert route renamed its parameter to avoid a binding collision.
        if (TryGetRouteGuid(context, CaseIdRouteKey, out var caseId) ||
            TryGetRouteGuid(context, RouteCaseIdRouteKey, out caseId))
        {
            await permissionService.EnsureHeldOverCaseAsync(user, Permission, new CaseId(caseId), stoppingToken);

            return;
        }

        if (TryGetRouteGuid(context, QueueIdRouteKey, out var queueId))
        {
            await permissionService.EnsureHeldOverQueueAsync(user, Permission, new QueueId(queueId), stoppingToken);
        }
    }

    private static bool TryGetRouteGuid(ActionExecutingContext context, string key, out Guid value)
    {
        var routeValue = context.RouteData.Values
            .FirstOrDefault(x => string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase))
            .Value?
            .ToString();

        return Guid.TryParse(routeValue, out value);
    }

    private bool IsSupersededByAction(ActionExecutingContext context)
    {
        if (context.ActionDescriptor is not ControllerActionDescriptor { MethodInfo: var methodInfo })
        {
            return false;
        }

        var declared = ActionPermissions.GetOrAdd(
            methodInfo,
            static method => method
                .GetCustomAttributes<RequirePermissionAttribute>(false)
                .Select(x => x.Permission)
                .ToArray());

        return declared.Length > 0 && Array.IndexOf(declared, Permission) < 0;
    }
}
