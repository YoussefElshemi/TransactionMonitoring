using Core.Models.Permissions;
using Core.ValueObjects;

namespace Core.Interfaces.Repositories;

public interface IPermissionScopeRepository
{
    Task<ResourceScope?> FindCaseScopeAsync(CaseId caseId, CancellationToken stoppingToken);
    Task<ResourceScope?> FindQueueScopeAsync(QueueId queueId, CancellationToken stoppingToken);
    Task<ResourceScope?> FindRuleScopeAsync(RuleId ruleId, CancellationToken stoppingToken);
    Task<ResourceScope?> FindRuleSetScopeAsync(RuleSetId ruleSetId, CancellationToken stoppingToken);

    Task<Exists> IsScopeWithinOrganizationAsync(OrganizationId organizationId, PermissionScope scope, CancellationToken stoppingToken);

    Task<List<QueueId>> ListQueueIdsInSubOrganizationAsync(SubOrganizationId subOrganizationId, CancellationToken stoppingToken);
    Task<List<RuleSetId>> ListRuleSetIdsInSubOrganizationAsync(SubOrganizationId subOrganizationId, CancellationToken stoppingToken);
}
