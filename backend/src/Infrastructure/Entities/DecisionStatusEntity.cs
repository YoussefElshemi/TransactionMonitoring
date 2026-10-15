using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record DecisionStatusEntity : BaseEntity
{
    public required int DecisionStatusId { get; init; }
    public required string Name { get; init; }
    public ICollection<DecisionEntity> Decisions { get; init; } = [];
}
