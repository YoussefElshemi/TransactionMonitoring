using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record DecisionReasonCodeEntity : BaseEntity
{
    public required int DecisionReasonCodeId { get; init; }
    public required string Name { get; init; }
    public ICollection<DecisionEntity> Decisions { get; init; } = [];
}
