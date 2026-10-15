using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record RuleDeploymentStatusEntity : BaseEntity
{
    public required int RuleDeploymentStatusId { get; init; }
    public required string Name { get; init; }
    public ICollection<RuleDeploymentEntity> RuleDeployments { get; init; } = [];
}
