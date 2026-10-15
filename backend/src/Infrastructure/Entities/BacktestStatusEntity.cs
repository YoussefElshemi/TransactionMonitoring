using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record BackTestStatusEntity : BaseEntity
{
    public required int BackTestStatusId { get; init; }
    public required string Name { get; init; }
    public ICollection<BackTestEntity> BackTests { get; init; } = [];
}
