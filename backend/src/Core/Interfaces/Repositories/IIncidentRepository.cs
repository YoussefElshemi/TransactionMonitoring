using Core.Enums;
using Core.Models.Status;
using Core.ValueObjects;

namespace Core.Interfaces.Repositories;

public interface IIncidentRepository : IPagedRepository<ListIncidentRequest, Incident>
{
    Task AddAsync(Incident incident, CancellationToken stoppingToken);
    Task<Incident> GetAsync(IncidentId incidentId, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(IncidentId incidentId, CancellationToken stoppingToken);

    Task<List<Incident>> ListActiveAsync(CancellationToken stoppingToken);

    Task<List<Incident>> ListActiveForComponentAsync(ServiceComponent component, CancellationToken stoppingToken);

    Task UpdateAsync(Incident incident, CancellationToken stoppingToken);
    Task AddUpdateAsync(IncidentUpdate incidentUpdate, CancellationToken stoppingToken);
    Task<IncidentNumber> GenerateIncidentNumberAsync(CancellationToken stoppingToken);
}
