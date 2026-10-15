using Core.Models.RuleEvaluations;
using Core.ValueObjects;
using Presentation.Mappers.RuleEvaluations;
using Presentation.Models.RuleEvaluations;

namespace Presentation.Factories;

public class RuleEvaluationResponseFactory(RelatedRecordLoader relatedRecordLoader)
{
    public async Task<RuleEvaluationResourceDto> CreateAsync(
        RuleEvaluation ruleEvaluation, EnrichmentContext context, CancellationToken stoppingToken)
    {
        var resources = await CreateAsync([ruleEvaluation], context, stoppingToken);

        return resources[0];
    }

    public async Task<RuleEvaluationResourceDto[]> CreateAsync(
        IReadOnlyCollection<RuleEvaluation> ruleEvaluations, EnrichmentContext context, CancellationToken stoppingToken)
    {
        if (ruleEvaluations.Count == 0)
        {
            return [];
        }

        var rules = await relatedRecordLoader.LoadRulesAsync(
            ruleEvaluations.Select(x => (RuleId?)x.RuleId), context, stoppingToken);
        var organizations = await relatedRecordLoader.LoadOrganizationsAsync(
            ruleEvaluations.Select(x => (OrganizationId?)x.OrganizationId), context, stoppingToken);
        var transactions = await relatedRecordLoader.LoadTransactionsAsync(
            ruleEvaluations.Select(x => (TransactionId?)x.TransactionId), context, stoppingToken);

        return ruleEvaluations.Select(x => new RuleEvaluationResourceDto
        {
            RuleEvaluation = x.ToDto(),
            Rule = rules.GetValueOrDefault(x.RuleId),
            Organization = organizations.GetValueOrDefault(x.OrganizationId),
            Transaction = transactions.GetValueOrDefault(x.TransactionId)
        }).ToArray();
    }
}
