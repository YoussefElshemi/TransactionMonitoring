using Core.Enums;
using Core.Models.Status;
using Core.ValueObjects;

namespace Core.Interfaces.Services;

public interface IStatusService
{
    Task<ServiceStatus> GetAsync(CancellationToken stoppingToken);

    Task<ComponentProbe> RecordProbeAsync(RecordComponentProbeRequest recordComponentProbeRequest, CancellationToken stoppingToken);

    Task<Count> RollUpDayAsync(ObservationDate date, CancellationToken stoppingToken);

    Task<ServiceHeartbeat> GetHeartbeatAsync(CancellationToken stoppingToken);

    Task<ComponentStatus> GetComponentStatusAsync(ServiceComponent component, CancellationToken stoppingToken);

    Task<List<ComponentProbe>> ListRecentProbesAsync(ServiceComponent component, Count count, CancellationToken stoppingToken);
    Task<Count> PurgeProbesAsync(CancellationToken stoppingToken);
}
