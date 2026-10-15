using Core.Models.Decisions;

namespace Core.Interfaces.Repositories;

public interface IDecisionRuleLinkRepository
{
    Task AddRangeAsync(List<DecisionRuleEvaluationLink> decisionRuleLinks, CancellationToken stoppingToken);
}
