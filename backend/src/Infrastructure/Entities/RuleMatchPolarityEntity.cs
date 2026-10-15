using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record RuleMatchPolarityEntity : BaseEntity
{
    public required int RuleMatchPolarityId { get; init; }
    public required string Name { get; init; }
    public ICollection<RuleEntity> Rules { get; init; } = [];
    public ICollection<RuleVersionEntity> RuleVersions { get; init; } = [];
}
