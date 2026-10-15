using Core.Models.Rules;
using Core.Models.RuleSets;
using Core.ValueObjects;

namespace Core.Interfaces.Services;

public interface IRuleSetService : IPagedService<ListRuleSetRequest, RuleSet>
{
    Task<RuleSet> CreateAsync(CreateRuleSetRequest createRuleSetRequest, CancellationToken stoppingToken);
    Task UpdateAsync(UpdateRuleSetRequest updateRuleSetRequest, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(OrganizationId organizationId, RuleSetId ruleSetId, IncludeDeleted includeDeleted, CancellationToken stoppingToken);
    Task<RuleSet> GetAsync(RuleSetId ruleSetId, CancellationToken stoppingToken);
    Task<List<RuleSet>> ListByIdsAsync(IReadOnlyCollection<RuleSetId> ruleSetIds, CancellationToken stoppingToken);
    Task DeleteAsync(RuleSetId ruleSetId, CancellationToken stoppingToken);
    Task RecoverAsync(RuleSetId ruleSetId, CancellationToken stoppingToken);
    Task<EvaluateRuleSetResponse> EvaluateAsync(EvaluateRuleSetRequest evaluateRuleSetRequest, CancellationToken stoppingToken);
    Task<EvaluateAllRulesResponse> EvaluateAllAsync(EvaluateRuleSetsRequest evaluateRuleSetsRequest, CancellationToken stoppingToken);
}