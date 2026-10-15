using Core.Models.BackTests;
using Core.Models.RuleEvaluations;
using Core.ValueObjects;

namespace Core.Interfaces.Repositories;

public interface IRuleEvaluationRepository : IPagedRepository<ListRuleEvaluationRequest, RuleEvaluation>
{
    Task AddAsync(RuleEvaluation ruleEvaluation, CancellationToken stoppingToken);
    Task AddRangeAsync(List<RuleEvaluation> ruleEvaluations, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(OrganizationId organizationId, RuleEvaluationId ruleEvaluationId, IncludeDeleted includeDeleted, CancellationToken stoppingToken);
    Task<RuleEvaluation> GetAsync(RuleEvaluationId ruleEvaluationId, CancellationToken stoppingToken);
    Task<TotalCount> CountByRuleAsync(OrganizationId organizationId, RuleId ruleId, CancellationToken stoppingToken);
    Task<List<BackTestSample>> ListChunkForBackTestAsync(OrganizationId organizationId, RuleId ruleId, BackTestId backTestId, int chunkSize, CancellationToken stoppingToken);
}
