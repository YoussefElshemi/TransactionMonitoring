using Core.Models.RuleDeployments;
using Core.Models.Rules;
using Core.ValueObjects;

namespace Core.Interfaces.Services;

public interface IRuleDeploymentService : IPagedService<ListRuleDeploymentRequest, RuleDeployment>
{
    Task<RuleDeployment> DeployAsync(DeployRuleRequest deployRuleRequest, CancellationToken stoppingToken);

    Task<RuleDeployment> ExecuteApprovedAsync(RuleDeploymentId ruleDeploymentId, CancellationToken stoppingToken);
    Task CancelDeploymentAsync(RuleDeploymentId ruleDeploymentId, CancellationToken stoppingToken);
    Task<List<RuleDeployment>> ListByIdsAsync(IReadOnlyCollection<RuleDeploymentId> ruleDeploymentIds, CancellationToken stoppingToken);
    Task<Exists> ActiveDeploymentExistsAsync(RuleId ruleId, CancellationToken stoppingToken);

    Task<Exists> PendingApprovalExistsAsync(RuleId ruleId, CancellationToken stoppingToken);

    Task<Exists> DeploymentInProgressAsync(OrganizationId organizationId, RuleDeploymentId ruleDeploymentId, CancellationToken stoppingToken);
}
