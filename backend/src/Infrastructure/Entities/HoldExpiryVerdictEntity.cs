using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record HoldExpiryVerdictEntity : BaseEntity
{
    public required int HoldExpiryVerdictId { get; init; }
    public required string Name { get; init; }
    public ICollection<RuleEntity> Rules { get; init; } = [];
    public ICollection<OrganizationEntity> Organizations { get; init; } = [];
    public ICollection<DecisionEntity> Decisions { get; init; } = [];
}
