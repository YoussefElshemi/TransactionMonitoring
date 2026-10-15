using Core.Enums;
using Core.Models.BaseModels;
using Core.ValueObjects;

namespace Core.Models.RuleDeployments;

public record RuleDeployment : BaseModel
{
    public required RuleDeploymentId RuleDeploymentId { get; init; }
    public required OrganizationId OrganizationId { get; init; }
    public required RuleId RuleId { get; init; }
    public required RuleVersionId TargetRuleVersionId { get; init; }
    public required RuleVersionId? PreviousRuleVersionId { get; init; }
    public required DeploymentStrategy Strategy { get; init; }
    public required RuleDeploymentStatus Status { get; init; }
    public required RolloutPercentage CurrentPercentage { get; init; }
    public required StepPercentage? StepPercentage { get; init; }
    public required StepIntervalSeconds? StepIntervalSeconds { get; init; }
    public required ApplicationUserId CreatedByUserId { get; init; }
    public required CompletedAt? CompletedAt { get; init; }

    public required Mode Mode { get; init; }
}
