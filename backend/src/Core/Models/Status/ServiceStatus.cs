using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.Status;

public record ServiceStatus
{
    public required ComponentStatus Status { get; init; }
    public required ObservedAt ObservedAt { get; init; }
    public required List<ComponentHealth> Components { get; init; }
    public required List<Incident> ActiveIncidents { get; init; }
    public required List<MaintenanceWindow> UpcomingMaintenance { get; init; }
}
