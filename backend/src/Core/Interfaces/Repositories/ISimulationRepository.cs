using Core.Models.Common;
using Core.Models.Simulations;
using Core.ValueObjects;

namespace Core.Interfaces.Repositories;

public interface ISimulationRepository
{
    Task AddAsync(Simulation simulation, CancellationToken stoppingToken);

    Task<Simulation> GetAsync(SimulationId simulationId, CancellationToken stoppingToken);

    Task<Exists> ExistsAsync(OrganizationId organizationId, SimulationId simulationId, CancellationToken stoppingToken);

    Task<Exists> HasRunningAsync(OrganizationId organizationId, CancellationToken stoppingToken);

    Task UpdateAsync(UpdateSimulationRequest updateSimulationRequest, CancellationToken stoppingToken);

    Task<PagedResponse<Simulation>> ListAsync(ListSimulationRequest listSimulationRequest, CancellationToken stoppingToken);

    Task<TotalCount> GetCountAsync(ListSimulationRequest listSimulationRequest, CancellationToken stoppingToken);
}
