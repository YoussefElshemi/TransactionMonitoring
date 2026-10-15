using Core.Models.RuleEvaluations;
using Core.ValueObjects;

namespace Core.Interfaces.Services;

public interface IRuleEvaluationService : IPagedService<ListRuleEvaluationRequest, RuleEvaluation>
{
    Task<RuleEvaluation> CreateAsync(CreateRuleEvaluationRequest createRuleEvaluationRequest, CancellationToken stoppingToken);
    Task<List<RuleEvaluation>> CreateManyAsync(List<CreateRuleEvaluationRequest> createRuleEvaluationRequests, CancellationToken stoppingToken);
    Task<RuleEvaluation> GetAsync(RuleEvaluationId caseId, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(OrganizationId organizationId, RuleEvaluationId caseId, IncludeDeleted includeDeleted, CancellationToken stoppingToken);
}