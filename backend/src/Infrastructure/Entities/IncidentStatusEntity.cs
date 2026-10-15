using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record IncidentStatusEntity : BaseEntity
{
    public required int IncidentStatusId { get; init; }
    public required string Name { get; init; }
}
