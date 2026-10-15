using Core.Enums;
using Core.Models.Status;
using Core.ValueObjects;

namespace Core.Interfaces.Repositories;

public interface IComponentProbeRepository
{
    Task AddAsync(ComponentProbe componentProbe, CancellationToken stoppingToken);

    Task<List<ComponentProbe>> ListRecentAsync(ServiceComponent component, Count count, CancellationToken stoppingToken);

    Task<List<ComponentProbe>> ListForDayAsync(ServiceComponent component, ObservationDate date, CancellationToken stoppingToken);
    Task<Count> PurgeOlderThanAsync(ObservedAt threshold, CancellationToken stoppingToken);
}
