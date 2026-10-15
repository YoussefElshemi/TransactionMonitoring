using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record ProbeSourceEntity : BaseEntity
{
    public required int ProbeSourceId { get; init; }
    public required string Name { get; init; }
}
