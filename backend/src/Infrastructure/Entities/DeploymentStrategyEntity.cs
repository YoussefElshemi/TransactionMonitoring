using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record DeploymentStrategyEntity : BaseEntity
{
    public required int DeploymentStrategyId { get; init; }
    public required string Name { get; init; }
    public ICollection<RuleDeploymentEntity> RuleDeployments { get; init; } = [];
}
