using Core.Models.Simulations;
using Core.ValueObjects;
using Presentation.Mappers.Simulations;
using Presentation.Models.Simulations;

namespace Presentation.Factories;

public class SimulationResponseFactory(RelatedRecordLoader relatedRecordLoader)
{
    public async Task<SimulationResourceDto> CreateAsync(Simulation simulation, EnrichmentContext context, CancellationToken stoppingToken)
    {
        var resources = await CreateAsync([simulation], context, stoppingToken);

        return resources[0];
    }

    public async Task<SimulationResourceDto[]> CreateAsync(
        IReadOnlyCollection<Simulation> simulations, EnrichmentContext context, CancellationToken stoppingToken)
    {
        if (simulations.Count == 0)
        {
            return [];
        }

        var organizations = await relatedRecordLoader.LoadOrganizationsAsync(
            simulations.Select(x => (OrganizationId?)x.OrganizationId), context, stoppingToken);
        var createdByUsers = await relatedRecordLoader.LoadUsersAsync(
            simulations.Select(x => (ApplicationUserId?)x.CreatedByUserId), context, stoppingToken);

        return simulations.Select(x => new SimulationResourceDto
        {
            Simulation = x.ToDto(),
            Organization = organizations.GetValueOrDefault(x.OrganizationId),
            CreatedByUser = createdByUsers.GetValueOrDefault(x.CreatedByUserId)
        }).ToArray();
    }
}
