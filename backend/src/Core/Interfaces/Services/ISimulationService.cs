using Core.Models.Simulations;
using Core.ValueObjects;

namespace Core.Interfaces.Services;

public interface ISimulationService : IPagedService<ListSimulationRequest, Simulation>
{
    Task<Simulation> StartAsync(StartSimulationRequest startSimulationRequest, CancellationToken stoppingToken);

    Task<Simulation> GetAsync(SimulationId simulationId, CancellationToken stoppingToken);

    Task<Exists> ExistsAsync(OrganizationId organizationId, SimulationId simulationId, CancellationToken stoppingToken);

    Task<Exists> HasRunningAsync(OrganizationId organizationId, CancellationToken stoppingToken);

    Task UpdateAsync(UpdateSimulationRequest updateSimulationRequest, CancellationToken stoppingToken);
}
