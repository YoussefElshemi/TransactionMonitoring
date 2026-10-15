using Core.Models.VelocityProfiles;
using Core.ValueObjects;
using Presentation.Mappers.VelocityProfiles;
using Presentation.Models.VelocityProfiles;

namespace Presentation.Factories;

public class VelocityProfileResponseFactory(RelatedRecordLoader relatedRecordLoader)
{
    public async Task<VelocityProfileResourceDto> CreateAsync(
        VelocityProfile velocityProfile, EnrichmentContext context, CancellationToken stoppingToken)
    {
        var organizations = await relatedRecordLoader.LoadOrganizationsAsync(
            [velocityProfile.OrganizationId], context, stoppingToken);

        return new VelocityProfileResourceDto
        {
            VelocityProfile = velocityProfile.ToDto(),
            Organization = organizations.GetValueOrDefault(velocityProfile.OrganizationId)
        };
    }

    public async Task<VelocityBackfillResourceDto[]> CreateAsync(
        IReadOnlyCollection<VelocityBackfill> velocityBackfills, EnrichmentContext context, CancellationToken stoppingToken)
    {
        if (velocityBackfills.Count == 0)
        {
            return [];
        }

        var organizations = await relatedRecordLoader.LoadOrganizationsAsync(
            velocityBackfills.Select(x => (OrganizationId?)x.OrganizationId), context, stoppingToken);

        return velocityBackfills.Select(x => new VelocityBackfillResourceDto
        {
            VelocityBackfill = x.ToDto(),
            Organization = organizations.GetValueOrDefault(x.OrganizationId)
        }).ToArray();
    }
}
