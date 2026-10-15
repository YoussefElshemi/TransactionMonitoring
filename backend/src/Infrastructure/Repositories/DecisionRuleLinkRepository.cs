using Core.Interfaces.Repositories;
using Core.Models.Decisions;
using Infrastructure.Mappers;

namespace Infrastructure.Repositories;

public class DecisionRuleLinkRepository(TransactionMonitoringDbContext context) : IDecisionRuleLinkRepository
{
    public async Task AddRangeAsync(List<DecisionRuleEvaluationLink> decisionRuleLinks, CancellationToken stoppingToken)
    {
        var decisionRuleLinkEntities = decisionRuleLinks.Select(x => x.ToEntity());
        await context.DecisionRuleLinks.AddRangeAsync(decisionRuleLinkEntities, stoppingToken);
    }
}
