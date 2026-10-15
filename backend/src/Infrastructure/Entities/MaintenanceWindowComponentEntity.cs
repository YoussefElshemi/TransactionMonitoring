using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record MaintenanceWindowComponentEntity : BaseEntity
{
    public required Guid MaintenanceWindowId { get; init; }
    public MaintenanceWindowEntity MaintenanceWindow { get; init; } = null!;
    public required int ComponentId { get; init; }
    public ServiceComponentEntity Component { get; init; } = null!;
}
