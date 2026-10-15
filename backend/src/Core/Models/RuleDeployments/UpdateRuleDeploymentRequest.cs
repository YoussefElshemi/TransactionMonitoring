using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.RuleDeployments;

public record UpdateRuleDeploymentRequest
{
    public required RuleDeploymentId RuleDeploymentId { get; init; }
    public required RuleDeploymentStatus Status { get; init; }
    public required RolloutPercentage CurrentPercentage { get; init; }
    public required CompletedAt? CompletedAt { get; init; }
}
