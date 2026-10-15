using Core.Models.RuleDeployments;
using Core.ValueObjects;
using Presentation.Mappers.Rules;
using Presentation.Models.Rules;

namespace Presentation.Factories;

public class RuleDeploymentResponseFactory(RelatedRecordLoader relatedRecordLoader)
{
    public async Task<RuleDeploymentResourceDto> CreateAsync(
        RuleDeployment ruleDeployment, EnrichmentContext context, CancellationToken stoppingToken)
    {
        var resources = await CreateAsync([ruleDeployment], context, stoppingToken);

        return resources[0];
    }

    public async Task<RuleDeploymentResourceDto[]> CreateAsync(
        IReadOnlyCollection<RuleDeployment> ruleDeployments, EnrichmentContext context, CancellationToken stoppingToken)
    {
        if (ruleDeployments.Count == 0)
        {
            return [];
        }

        var rules = await relatedRecordLoader.LoadRulesAsync(
            ruleDeployments.Select(x => (RuleId?)x.RuleId), context, stoppingToken);
        var createdByUsers = await relatedRecordLoader.LoadUsersAsync(
            ruleDeployments.Select(x => (ApplicationUserId?)x.CreatedByUserId), context, stoppingToken);
        var ruleVersions = await relatedRecordLoader.LoadRuleVersionsAsync(
            ruleDeployments
                .Select(x => (RuleVersionId?)x.TargetRuleVersionId)
                .Concat(ruleDeployments.Select(x => x.PreviousRuleVersionId)),
            context,
            stoppingToken);

        return ruleDeployments.Select(x => new RuleDeploymentResourceDto
        {
            RuleDeployment = x.ToDto(),
            Rule = rules.GetValueOrDefault(x.RuleId),
            CreatedByUser = createdByUsers.GetValueOrDefault(x.CreatedByUserId),
            TargetRuleVersion = ruleVersions.GetValueOrDefault(x.TargetRuleVersionId),
            PreviousRuleVersion = x.PreviousRuleVersionId.HasValue
                ? ruleVersions.GetValueOrDefault(x.PreviousRuleVersionId.Value)
                : null
        }).ToArray();
    }
}
