using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record MaintenanceStatusEntity : BaseEntity
{
    public required int MaintenanceStatusId { get; init; }
    public required string Name { get; init; }
}
