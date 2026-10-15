using Core.Models.Rules;
using Core.ValueObjects;

namespace Core.Interfaces.Services;

public interface IRuleService : IPagedService<ListRuleRequest, Rule>
{
    Task<Rule> CreateAsync(CreateRuleRequest createRuleRequest, CancellationToken stoppingToken);
    Task<Rule> UpdateAsync(UpdateRuleRequest updateRuleRequest, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(OrganizationId organizationId, RuleId ruleId, IncludeDeleted includeDeleted, CancellationToken stoppingToken);
    Task<Rule> GetAsync(RuleId ruleId, CancellationToken stoppingToken);
    Task<List<Rule>> ListByIdsAsync(IReadOnlyCollection<RuleId> ruleIds, CancellationToken stoppingToken);
    Task DeleteAsync(RuleId ruleId, CancellationToken stoppingToken);
    Task RecoverAsync(RuleId ruleId, CancellationToken stoppingToken);
    Task<EvaluateRuleResponse> EvaluateAsync(EvaluateRuleRequest evaluateRuleRequest, CancellationToken stoppingToken);
    Task<TestRuleResponse> TestAsync(TestRuleRequest testRuleRequest, CancellationToken stoppingToken);
    Task MoveAsync(RuleId ruleId, RuleSetId targetRuleSetId, CancellationToken stoppingToken);
    Task<Rule> CopyRuleAsync(CopyRuleRequest copyRuleRequest, CancellationToken stoppingToken);
}
