using System.Security.Claims;
using Core.Enums;
using Core.Models.Permissions;
using Core.ValueObjects;

namespace Core.Interfaces.Services;

public interface IPermissionService
{
    Task<EffectivePermissions> ResolveAsync(ApplicationUserId userId, CancellationToken stoppingToken);
    Task<EffectivePermissions> ResolveAsync(ClaimsPrincipal claimsPrincipal, CancellationToken stoppingToken);

    Task EnsureHeldAsync(ClaimsPrincipal claimsPrincipal, Permission permission, CancellationToken stoppingToken);

    Task EnsureHeldOverCaseAsync(ClaimsPrincipal claimsPrincipal, Permission permission, CaseId caseId, CancellationToken stoppingToken);
    Task EnsureHeldOverQueueAsync(ClaimsPrincipal claimsPrincipal, Permission permission, QueueId queueId, CancellationToken stoppingToken);
    Task EnsureHeldOverRuleAsync(ClaimsPrincipal claimsPrincipal, Permission permission, RuleId ruleId, CancellationToken stoppingToken);
    Task EnsureHeldOverRuleSetAsync(ClaimsPrincipal claimsPrincipal, Permission permission, RuleSetId ruleSetId, CancellationToken stoppingToken);

    Task EnsureOrganizationReachableAsync(ClaimsPrincipal claimsPrincipal, OrganizationId organizationId, CancellationToken stoppingToken);

    Task<PermissionFilter?> ResolveFilterAsync(ClaimsPrincipal claimsPrincipal, Permission permission, CancellationToken stoppingToken);
}
