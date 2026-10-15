using Core.Models.Rules;
using Core.ValueObjects;

namespace Core.Interfaces.Repositories;

public interface IRuleRepository : IPagedRepository<ListRuleRequest, Rule>
{
    Task AddAsync(Rule rule, CancellationToken stoppingToken);
    Task UpdateAsync(UpdateRuleRequest updateRuleRequest, CancellationToken stoppingToken);
    Task DeployVersionAsync(RuleId ruleId, RuleVersion targetRuleVersion, CancellationToken stoppingToken);
    Task MoveAsync(RuleId ruleId, RuleSetId targetRuleSetId, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(OrganizationId organizationId, RuleSetId ruleSetId, RuleId ruleId, IncludeDeleted includeDeleted, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(OrganizationId organizationId, RuleId ruleId, IncludeDeleted includeDeleted, CancellationToken stoppingToken);
    Task<Rule> GetAsync(RuleId ruleId, CancellationToken stoppingToken);
    Task<List<Rule>> ListByIdsAsync(IReadOnlyCollection<RuleId> ruleIds, CancellationToken stoppingToken);
    Task<List<Rule>> ListByOrganizationAsync(OrganizationId organizationId, CancellationToken stoppingToken);
    Task DeleteAsync(RuleId ruleId, CancellationToken stoppingToken);
    Task RecoverAsync(RuleId ruleId, CancellationToken stoppingToken);
}