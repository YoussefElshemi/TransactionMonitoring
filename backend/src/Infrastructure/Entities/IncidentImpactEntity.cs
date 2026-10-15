using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record IncidentImpactEntity : BaseEntity
{
    public required int IncidentImpactId { get; init; }
    public required string Name { get; init; }
}
