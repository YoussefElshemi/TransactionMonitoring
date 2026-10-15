using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record RuleActionEntity : BaseEntity
{
    public required int RuleActionId { get; init; }
    public required string Name { get; init; }
    public ICollection<RuleEntity> Rules { get; init; } = [];
    public ICollection<RuleVersionEntity> RuleVersions { get; init; } = [];
}
