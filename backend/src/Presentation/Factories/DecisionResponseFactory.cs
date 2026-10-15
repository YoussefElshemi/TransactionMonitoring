using Core.Models.Decisions;
using Core.ValueObjects;
using Presentation.Mappers.Decisions;
using Presentation.Models.Decisions;

namespace Presentation.Factories;

public class DecisionResponseFactory(RelatedRecordLoader relatedRecordLoader)
{
    public async Task<DecisionResourceDto> CreateAsync(Decision decision, EnrichmentContext context, CancellationToken stoppingToken)
    {
        var resources = await CreateAsync([decision], context, stoppingToken);

        return resources[0];
    }

    public async Task<DecisionResourceDto[]> CreateAsync(
        IReadOnlyCollection<Decision> decisions, EnrichmentContext context, CancellationToken stoppingToken)
    {
        if (decisions.Count == 0)
        {
            return [];
        }

        var resolvedByUsers = await relatedRecordLoader.LoadUsersAsync(
            decisions.Select(x => x.ResolvedByUserId), context, stoppingToken);
        var rules = await relatedRecordLoader.LoadRulesAsync(
            decisions.SelectMany(x => x.RuleEvaluations).Select(x => (RuleId?)x.RuleId), context, stoppingToken);

        return decisions.Select(x => new DecisionResourceDto
        {
            Decision = x.ToDto(),
            ResolvedByUser = x.ResolvedByUserId.HasValue ? resolvedByUsers.GetValueOrDefault(x.ResolvedByUserId.Value) : null,
            Rules = ResourceRelations.Resolve(x.RuleEvaluations.Select(evaluation => evaluation.RuleId), rules)
        }).ToArray();
    }
}
