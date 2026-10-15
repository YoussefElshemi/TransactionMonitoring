using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.Status;

public record UpdateMaintenanceWindowRequest
{
    public required MaintenanceWindowId MaintenanceWindowId { get; init; }
    public required MaintenanceWindowTitle Title { get; init; }
    public required MaintenanceWindowDescription Description { get; init; }
    public required List<ServiceComponent> Components { get; init; }
    public required ScheduledStart ScheduledStart { get; init; }
    public required ScheduledEnd ScheduledEnd { get; init; }
}
