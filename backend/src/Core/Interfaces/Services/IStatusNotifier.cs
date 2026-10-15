using Core.Models.Status;
using Core.ValueObjects;

namespace Core.Interfaces.Services;

public interface IStatusNotifier
{
    Task<Count> NotifySubscribersAsync(Incident incident, IncidentUpdate incidentUpdate, CancellationToken stoppingToken);

    Task<Count> NotifySubscribersAsync(MaintenanceWindow maintenanceWindow, CancellationToken stoppingToken);
}
