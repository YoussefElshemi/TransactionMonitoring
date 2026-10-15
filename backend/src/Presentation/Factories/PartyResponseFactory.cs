using Core.Models.Parties;
using Core.ValueObjects;
using Presentation.Mappers.Parties;
using Presentation.Models.Parties;

namespace Presentation.Factories;

public class PartyResponseFactory(RelatedRecordLoader relatedRecordLoader)
{
    public async Task<PartyResourceDto> CreateAsync(
        Party party, EnrichmentContext context, CancellationToken stoppingToken)
    {
        var resources = await CreateAsync([party], context, stoppingToken);

        return resources[0];
    }

    public async Task<PartyResourceDto[]> CreateAsync(
        IReadOnlyCollection<Party> parties, EnrichmentContext context, CancellationToken stoppingToken)
    {
        if (parties.Count == 0)
        {
            return [];
        }

        var organizations = await relatedRecordLoader.LoadOrganizationsAsync(
            parties.Select(x => (OrganizationId?)x.OrganizationId), context, stoppingToken);

        return parties.Select(x => new PartyResourceDto
        {
            Party = x.ToDto(),
            Organization = organizations.GetValueOrDefault(x.OrganizationId)
        }).ToArray();
    }
}
