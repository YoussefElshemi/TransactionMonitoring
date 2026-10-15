using System.Security.Claims;
using Core.Enums;
using Core.Exceptions;
using Core.Helpers;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Models.Permissions;
using Core.Models.Users;
using Core.ValueObjects;

namespace Core.Services;

// Memoised per request: a single request asks several times, and scoped lifetime means revocations apply on the next call.
public class PermissionService(
    IApplicationUserRepository applicationUserRepository,
    IRoleAssignmentRepository roleAssignmentRepository,
    IPermissionScopeRepository permissionScopeRepository) : IPermissionService
{
    private readonly Dictionary<ApplicationUserId, EffectivePermissions> _resolved = [];

    public async Task<EffectivePermissions> ResolveAsync(ApplicationUserId userId, CancellationToken stoppingToken)
    {
        if (_resolved.TryGetValue(userId, out var cached))
        {
            return cached;
        }

        var user = await applicationUserRepository.GetByIdAsync(userId, stoppingToken);
        var assignments = await roleAssignmentRepository.ListForUserAsync(userId, stoppingToken);

        var grants = new List<PermissionGrant>();

        foreach (var permission in PermissionBundle.For(user.Roles))
        {
            grants.Add(new PermissionGrant
            {
                Permission = permission,
                Scope = PermissionScope.Organization()
            });
        }

        foreach (var assignment in assignments)
        {
            if (assignment.Role is not { } role || role.IsDeleted)
            {
                continue;
            }

            foreach (var permission in role.Permissions)
            {
                grants.Add(new PermissionGrant
                {
                    Permission = permission,
                    Scope = assignment.Scope
                });
            }
        }

        var effectivePermissions = new EffectivePermissions
        {
            UserId = user.UserId,
            OrganizationId = user.OrganizationId,
            SubOrganizationId = user.SubOrganizationId,
            Grants = Narrow(grants, user)
        };

        _resolved[userId] = effectivePermissions;

        return effectivePermissions;
    }

    public async Task<EffectivePermissions> ResolveAsync(ClaimsPrincipal claimsPrincipal, CancellationToken stoppingToken)
    {
        var user = await applicationUserRepository.GetAsync(claimsPrincipal, stoppingToken);

        return await ResolveAsync(user.UserId, stoppingToken);
    }

    public async Task EnsureHeldAsync(ClaimsPrincipal claimsPrincipal, Permission permission, CancellationToken stoppingToken)
    {
        var effectivePermissions = await ResolveAsync(claimsPrincipal, stoppingToken);

        if (!effectivePermissions.Holds(permission))
        {
            throw new PermissionDeniedException(permission);
        }
    }

    public Task EnsureHeldOverCaseAsync(ClaimsPrincipal claimsPrincipal, Permission permission, CaseId caseId, CancellationToken stoppingToken)
    {
        return EnsureHeldOverAsync(claimsPrincipal, permission, () => permissionScopeRepository.FindCaseScopeAsync(caseId, stoppingToken), $"case {caseId}", stoppingToken);
    }

    public Task EnsureHeldOverQueueAsync(ClaimsPrincipal claimsPrincipal, Permission permission, QueueId queueId, CancellationToken stoppingToken)
    {
        return EnsureHeldOverAsync(claimsPrincipal, permission, () => permissionScopeRepository.FindQueueScopeAsync(queueId, stoppingToken), $"queue {queueId}", stoppingToken);
    }

    public Task EnsureHeldOverRuleAsync(ClaimsPrincipal claimsPrincipal, Permission permission, RuleId ruleId, CancellationToken stoppingToken)
    {
        return EnsureHeldOverAsync(claimsPrincipal, permission, () => permissionScopeRepository.FindRuleScopeAsync(ruleId, stoppingToken), $"rule {ruleId}", stoppingToken);
    }

    public Task EnsureHeldOverRuleSetAsync(ClaimsPrincipal claimsPrincipal, Permission permission, RuleSetId ruleSetId, CancellationToken stoppingToken)
    {
        return EnsureHeldOverAsync(claimsPrincipal, permission, () => permissionScopeRepository.FindRuleSetScopeAsync(ruleSetId, stoppingToken), $"rule set {ruleSetId}", stoppingToken);
    }

    public async Task EnsureOrganizationReachableAsync(ClaimsPrincipal claimsPrincipal, OrganizationId organizationId, CancellationToken stoppingToken)
    {
        var effectivePermissions = await ResolveAsync(claimsPrincipal, stoppingToken);

        if (effectivePermissions.OrganizationId == organizationId)
        {
            return;
        }

        if (!effectivePermissions.Holds(Permission.PlatformAdminister))
        {
            throw new PermissionDeniedException(Permission.PlatformAdminister, $"organization {organizationId}");
        }
    }

    public async Task<PermissionFilter?> ResolveFilterAsync(ClaimsPrincipal claimsPrincipal, Permission permission, CancellationToken stoppingToken)
    {
        var effectivePermissions = await ResolveAsync(claimsPrincipal, stoppingToken);

        if (effectivePermissions.IsUnscopedFor(permission))
        {
            return null;
        }

        var queueIds = new HashSet<QueueId>();
        var ruleSetIds = new HashSet<RuleSetId>();
        var subOrganizationIds = new HashSet<SubOrganizationId>();

        foreach (var scope in effectivePermissions.ScopesFor(permission))
        {
            switch (scope.ScopeType)
            {
                case PermissionScopeType.Queue when scope.QueueId.HasValue:
                    queueIds.Add(scope.QueueId.Value);
                    break;

                case PermissionScopeType.RuleSet when scope.RuleSetId.HasValue:
                    ruleSetIds.Add(scope.RuleSetId.Value);
                    break;

                case PermissionScopeType.SubOrganization when scope.SubOrganizationId.HasValue:
                    subOrganizationIds.Add(scope.SubOrganizationId.Value);

                    foreach (var queueId in await permissionScopeRepository.ListQueueIdsInSubOrganizationAsync(scope.SubOrganizationId.Value, stoppingToken))
                    {
                        queueIds.Add(queueId);
                    }

                    foreach (var ruleSetId in await permissionScopeRepository.ListRuleSetIdsInSubOrganizationAsync(scope.SubOrganizationId.Value, stoppingToken))
                    {
                        ruleSetIds.Add(ruleSetId);
                    }

                    break;
            }
        }

        return new PermissionFilter
        {
            QueueIds = [..queueIds],
            RuleSetIds = [..ruleSetIds],
            SubOrganizationIds = [..subOrganizationIds]
        };
    }

    private async Task EnsureHeldOverAsync(
        ClaimsPrincipal claimsPrincipal,
        Permission permission,
        Func<Task<ResourceScope?>> resolveScope,
        string scopeDescription,
        CancellationToken stoppingToken)
    {
        var effectivePermissions = await ResolveAsync(claimsPrincipal, stoppingToken);

        if (effectivePermissions.IsUnscopedFor(permission))
        {
            return;
        }

        var resourceScope = await resolveScope();

        if (resourceScope is null)
        {
            return;
        }

        if (!effectivePermissions.Holds(permission, resourceScope))
        {
            throw new PermissionDeniedException(permission, scopeDescription);
        }
    }

    private static List<PermissionGrant> Narrow(List<PermissionGrant> grants, ApplicationUser user)
    {
        if (user.SubOrganizationId is not { } subOrganizationId)
        {
            return grants;
        }

        return grants
            .Where(x => x.Permission != Permission.PlatformAdminister)
            .Select(x => new PermissionGrant
            {
                Permission = x.Permission,
                Scope = x.Scope.NarrowTo(subOrganizationId)
            })
            .ToList();
    }
}
