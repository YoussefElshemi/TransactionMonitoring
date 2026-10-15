using Core.Models.Rules;
using Core.ValueObjects;
using Presentation.Mappers.Rules;
using Presentation.Models.Rules;

namespace Presentation.Factories;

public class RuleResponseFactory(RelatedRecordLoader relatedRecordLoader)
{
    public async Task<RuleResourceDto> CreateAsync(Rule rule, EnrichmentContext context, CancellationToken stoppingToken)
    {
        var resources = await CreateAsync([rule], context, stoppingToken);

        return resources[0];
    }

    public async Task<RuleResourceDto[]> CreateAsync(
        IReadOnlyCollection<Rule> rules, EnrichmentContext context, CancellationToken stoppingToken)
    {
        if (rules.Count == 0)
        {
            return [];
        }

        var organizations = await relatedRecordLoader.LoadOrganizationsAsync(
            rules.Select(x => (OrganizationId?)x.OrganizationId), context, stoppingToken);
        var createdByUsers = await relatedRecordLoader.LoadUsersAsync(
            rules.Select(x => (ApplicationUserId?)x.CreatedByUserId), context, stoppingToken);
        var ruleSets = await relatedRecordLoader.LoadRuleSetsAsync(
            rules.Select(x => (RuleSetId?)x.RuleSetId), context, stoppingToken);

        return rules.Select(x => new RuleResourceDto
        {
            Rule = x.ToDto(),
            RuleSet = ruleSets.GetValueOrDefault(x.RuleSetId),
            Organization = organizations.GetValueOrDefault(x.OrganizationId),
            CreatedByUser = createdByUsers.GetValueOrDefault(x.CreatedByUserId)
        }).ToArray();
    }
}
