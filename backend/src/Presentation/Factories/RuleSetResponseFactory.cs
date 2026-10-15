using Core.Models.RuleSets;
using Core.ValueObjects;
using Presentation.Mappers.RuleSets;
using Presentation.Models.RuleSets;

namespace Presentation.Factories;

public class RuleSetResponseFactory(RelatedRecordLoader relatedRecordLoader)
{
    public async Task<RuleSetResourceDto> CreateAsync(RuleSet ruleSet, EnrichmentContext context, CancellationToken stoppingToken)
    {
        var resources = await CreateAsync([ruleSet], context, stoppingToken);

        return resources[0];
    }

    public async Task<RuleSetResourceDto[]> CreateAsync(
        IReadOnlyCollection<RuleSet> ruleSets, EnrichmentContext context, CancellationToken stoppingToken)
    {
        if (ruleSets.Count == 0)
        {
            return [];
        }

        var organizations = await relatedRecordLoader.LoadOrganizationsAsync(
            ruleSets.Select(x => (OrganizationId?)x.OrganizationId), context, stoppingToken);
        var createdByUsers = await relatedRecordLoader.LoadUsersAsync(
            ruleSets.Select(x => (ApplicationUserId?)x.CreatedByUserId), context, stoppingToken);

        return ruleSets.Select(x => new RuleSetResourceDto
        {
            RuleSet = x.ToDto(),
            Organization = organizations.GetValueOrDefault(x.OrganizationId),
            CreatedByUser = createdByUsers.GetValueOrDefault(x.CreatedByUserId)
        }).ToArray();
    }
}
