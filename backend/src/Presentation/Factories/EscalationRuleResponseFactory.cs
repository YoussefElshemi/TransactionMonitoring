using Core.Models.Escalations;
using Core.ValueObjects;
using Presentation.Mappers.Escalations;
using Presentation.Models.Escalations;

namespace Presentation.Factories;

public class EscalationRuleResponseFactory(RelatedRecordLoader relatedRecordLoader)
{
    public async Task<EscalationRuleResourceDto> CreateAsync(EscalationRule escalationRule, EnrichmentContext context, CancellationToken stoppingToken)
    {
        var resources = await CreateAsync([escalationRule], context, stoppingToken);

        return resources[0];
    }

    public async Task<EscalationRuleResourceDto[]> CreateAsync(
        IReadOnlyCollection<EscalationRule> escalationRules, EnrichmentContext context, CancellationToken stoppingToken)
    {
        if (escalationRules.Count == 0)
        {
            return [];
        }

        var organizations = await relatedRecordLoader.LoadOrganizationsAsync(
            escalationRules.Select(x => (OrganizationId?)x.OrganizationId), context, stoppingToken);

        return escalationRules.Select(x => new EscalationRuleResourceDto
        {
            EscalationRule = x.ToDto(),
            Organization = organizations.GetValueOrDefault(x.OrganizationId)
        }).ToArray();
    }
}
