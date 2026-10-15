using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.Rules;

public record DeployRuleRequest
{
    public required RuleId RuleId { get; init; }
    public required VersionNumber VersionNumber { get; init; }
    public required DeploymentStrategy Strategy { get; init; }
    public required StepPercentage? StepPercentage { get; init; }
    public required StepIntervalSeconds? StepIntervalSeconds { get; init; }
    public required ApplicationUserId DeployedByUserId { get; init; }
}
