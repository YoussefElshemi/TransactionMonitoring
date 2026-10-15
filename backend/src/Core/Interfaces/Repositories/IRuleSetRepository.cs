using Core.Models.RuleSets;
using Core.ValueObjects;

namespace Core.Interfaces.Repositories;

public interface IRuleSetRepository : IPagedRepository<ListRuleSetRequest, RuleSet>
{
    Task AddAsync(RuleSet ruleSet, CancellationToken stoppingToken);
    Task UpdateAsync(UpdateRuleSetRequest updateRuleSetRequest, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(OrganizationId organizationId, RuleSetId ruleSetId, IncludeDeleted includeDeleted, CancellationToken stoppingToken);
    Task<RuleSet> GetAsync(RuleSetId ruleSetId, CancellationToken stoppingToken);
    Task<ReviewThreshold?> GetReviewThresholdAsync(RuleSetId ruleSetId, CancellationToken stoppingToken);
    Task<List<RuleSet>> ListByIdsAsync(IReadOnlyCollection<RuleSetId> ruleSetIds, CancellationToken stoppingToken);
    Task<List<RuleSet>> GetAllAsync(OrganizationId organizationId, CancellationToken stoppingToken);
    Task DeleteAsync(RuleSetId ruleSetId, CancellationToken stoppingToken);
    Task RecoverAsync(RuleSetId ruleSetId, CancellationToken stoppingToken);
}