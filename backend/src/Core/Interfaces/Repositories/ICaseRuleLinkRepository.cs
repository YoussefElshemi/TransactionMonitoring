using Core.Models.Cases;

namespace Core.Interfaces.Repositories;

public interface ICaseRuleLinkRepository
{
    Task AddAsync(CaseRuleEvaluationLink caseRuleEvaluationLink, CancellationToken stoppingToken);
    Task AddRangeAsync(List<CaseRuleEvaluationLink> caseRuleLinks, CancellationToken stoppingToken);
}