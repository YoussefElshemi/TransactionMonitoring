using Core.Models.RuleDeployments;
using Core.ValueObjects;

namespace Core.Interfaces.Repositories;

public interface IRuleDeploymentRepository : IPagedRepository<ListRuleDeploymentRequest, RuleDeployment>
{
    Task AddAsync(RuleDeployment ruleDeployment, CancellationToken stoppingToken);
    Task<RuleDeployment> GetAsync(RuleDeploymentId ruleDeploymentId, CancellationToken stoppingToken);
    Task<RuleDeployment?> GetActiveByRuleIdAsync(RuleId ruleId, CancellationToken stoppingToken);
    Task<List<RuleDeployment>> ListByIdsAsync(IReadOnlyCollection<RuleDeploymentId> ruleDeploymentIds, CancellationToken stoppingToken);
    Task<Exists> ExistsActiveAsync(RuleId ruleId, CancellationToken stoppingToken);
    Task<Exists> ExistsPendingApprovalAsync(RuleId ruleId, CancellationToken stoppingToken);
    Task<Exists> ExistsInProgressAsync(OrganizationId organizationId, RuleDeploymentId ruleDeploymentId, CancellationToken stoppingToken);
    Task UpdateAsync(UpdateRuleDeploymentRequest request, CancellationToken stoppingToken);
}
