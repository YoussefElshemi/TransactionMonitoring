using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record VelocityBackfillStatusEntity : BaseEntity
{
    public required int VelocityBackfillStatusId { get; init; }
    public required string Name { get; init; }
    public ICollection<VelocityBackfillEntity> VelocityBackfills { get; init; } = [];
}
