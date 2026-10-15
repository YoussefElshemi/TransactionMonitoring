using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Mappers;
using Core.Models.Common;
using Core.Models.RuleEvaluations;
using Core.ValueObjects;

namespace Core.Services;

public class RuleEvaluationService(
    IRuleEvaluationRepository ruleEvaluationRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IModeContext modeContext) : IRuleEvaluationService
{
    public async Task<RuleEvaluation> CreateAsync(CreateRuleEvaluationRequest createRuleEvaluationRequest, CancellationToken stoppingToken)
    {
        var ruleEvaluation = createRuleEvaluationRequest.ToModel(timeProvider, modeContext.CurrentOrLive);

        await ruleEvaluationRepository.AddAsync(ruleEvaluation, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);

        return await GetAsync(ruleEvaluation.RuleEvaluationId, stoppingToken);
    }

    public async Task<List<RuleEvaluation>> CreateManyAsync(List<CreateRuleEvaluationRequest> createRuleEvaluationRequests, CancellationToken stoppingToken)
    {
        var ruleEvaluations = createRuleEvaluationRequests.Select(x => x.ToModel(timeProvider, modeContext.CurrentOrLive)).ToList();

        await ruleEvaluationRepository.AddRangeAsync(ruleEvaluations, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);

        return ruleEvaluations;
    }

    public Task<RuleEvaluation> GetAsync(RuleEvaluationId ruleEvaluationId, CancellationToken stoppingToken)
    {
        return ruleEvaluationRepository.GetAsync(ruleEvaluationId, stoppingToken);
    }

    public Task<Exists> ExistsAsync(OrganizationId organizationId, RuleEvaluationId ruleEvaluationId, IncludeDeleted includeDeleted, CancellationToken stoppingToken)
    {
        return ruleEvaluationRepository.ExistsAsync(organizationId, ruleEvaluationId, includeDeleted, stoppingToken);
    }

    public Task<PagedResponse<RuleEvaluation>> ListAsync(ListRuleEvaluationRequest listRuleEvaluationRequest, CancellationToken stoppingToken)
    {
        return ruleEvaluationRepository.ListAsync(listRuleEvaluationRequest, stoppingToken);
    }

    public Task<TotalCount> GetCountAsync(ListRuleEvaluationRequest listRuleEvaluationRequest, CancellationToken stoppingToken)
    {
        return ruleEvaluationRepository.GetCountAsync(listRuleEvaluationRequest, stoppingToken);
    }
}