using Core.Models.PartyMappings;
using Core.ValueObjects;
using Presentation.Mappers.PartyMappings;
using Presentation.Models.PartyMappings;

namespace Presentation.Factories;

public class PartyMappingResponseFactory(RelatedRecordLoader relatedRecordLoader)
{
    public async Task<PartyMappingResourceDto> CreateAsync(
        PartyMapping partyMapping, EnrichmentContext context, CancellationToken stoppingToken)
    {
        var organizations = await relatedRecordLoader.LoadOrganizationsAsync(
            [partyMapping.OrganizationId], context, stoppingToken);

        return new PartyMappingResourceDto
        {
            PartyMapping = partyMapping.ToDto(),
            Organization = organizations.GetValueOrDefault(partyMapping.OrganizationId)
        };
    }

    public async Task<PartyMappingVersionResourceDto> CreateAsync(
        PartyMappingVersion partyMappingVersion, EnrichmentContext context, CancellationToken stoppingToken)
    {
        var resources = await CreateAsync([partyMappingVersion], context, stoppingToken);

        return resources[0];
    }

    public async Task<PartyMappingVersionResourceDto[]> CreateAsync(
        IReadOnlyCollection<PartyMappingVersion> partyMappingVersions, EnrichmentContext context, CancellationToken stoppingToken)
    {
        if (partyMappingVersions.Count == 0)
        {
            return [];
        }

        var organizations = await relatedRecordLoader.LoadOrganizationsAsync(
            partyMappingVersions.Select(x => (OrganizationId?)x.OrganizationId), context, stoppingToken);

        return partyMappingVersions.Select(x => new PartyMappingVersionResourceDto
        {
            PartyMappingVersion = x.ToDto(),
            Organization = organizations.GetValueOrDefault(x.OrganizationId)
        }).ToArray();
    }
}
