using Core.Interfaces.Repositories;
using Core.Models.Cases;
using Infrastructure.Mappers;

namespace Infrastructure.Repositories;

public class CaseRuleLinkRepository(TransactionMonitoringDbContext context) : ICaseRuleLinkRepository
{
    public async Task AddAsync(CaseRuleEvaluationLink caseRuleEvaluationLink, CancellationToken stoppingToken)
    {
        var caseRuleLinkEntity = caseRuleEvaluationLink.ToEntity();
        await context.CaseRuleLinks.AddAsync(caseRuleLinkEntity, stoppingToken);
    }

    public async Task AddRangeAsync(List<CaseRuleEvaluationLink> caseRuleEvaluationLinks, CancellationToken stoppingToken)
    {
        var caseRuleLinkEntities = caseRuleEvaluationLinks.Select(x => x.ToEntity());
        await context.CaseRuleLinks.AddRangeAsync(caseRuleLinkEntities, stoppingToken);
    }
}