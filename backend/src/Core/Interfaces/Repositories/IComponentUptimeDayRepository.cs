using Core.Enums;
using Core.Models.Status;
using Core.ValueObjects;

namespace Core.Interfaces.Repositories;

public interface IComponentUptimeDayRepository
{
    Task UpsertAsync(ComponentUptimeDay componentUptimeDay, CancellationToken stoppingToken);

    Task<List<ComponentUptimeDay>> ListAsync(ObservationDate fromDate, ObservationDate toDate, CancellationToken stoppingToken);
    Task<ComponentUptimeDay?> GetAsync(ServiceComponent component, ObservationDate date, CancellationToken stoppingToken);
}
