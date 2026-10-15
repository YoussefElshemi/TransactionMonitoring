using Core.Enums;
using Core.Models.BaseModels;
using Core.ValueObjects;

namespace Core.Models.Status;

public record MaintenanceWindow : DeletableModel
{
    public required MaintenanceWindowId MaintenanceWindowId { get; init; }
    public required MaintenanceWindowTitle Title { get; init; }
    public required MaintenanceWindowDescription Description { get; init; }
    public required MaintenanceStatus Status { get; init; }
    public required List<ServiceComponent> Components { get; init; }
    public required ScheduledStart ScheduledStart { get; init; }
    public required ScheduledEnd ScheduledEnd { get; init; }
    public required AnnouncedAt? AnnouncedAt { get; init; }
    public required ApplicationUserId? CreatedByUserId { get; init; }
}
