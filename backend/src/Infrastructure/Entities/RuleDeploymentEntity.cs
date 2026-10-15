using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record RuleDeploymentEntity : BaseEntity, IModeScoped
{
    public required int ModeId { get; init; }

    public required Guid RuleDeploymentId { get; init; }
    public required Guid OrganizationId { get; init; }
    public OrganizationEntity Organization { get; init; } = null!;
    public required Guid RuleId { get; init; }
    public RuleEntity Rule { get; init; } = null!;
    public required Guid TargetRuleVersionId { get; init; }
    public RuleVersionEntity TargetRuleVersion { get; init; } = null!;
    public required Guid? PreviousRuleVersionId { get; init; }
    public RuleVersionEntity? PreviousRuleVersion { get; init; }
    public required int StrategyId { get; init; }
    public DeploymentStrategyEntity StrategyEntity { get; init; } = null!;
    public required int StatusId { get; set; }
    public RuleDeploymentStatusEntity StatusEntity { get; init; } = null!;
    public required int CurrentPercentage { get; set; }
    public required int? StepPercentage { get; init; }
    public required int? StepIntervalSeconds { get; init; }
    public required Guid CreatedByUserId { get; init; }
    public ApplicationUserEntity CreatedByUser { get; init; } = null!;
    public required DateTimeOffset? CompletedAt { get; set; }
}
