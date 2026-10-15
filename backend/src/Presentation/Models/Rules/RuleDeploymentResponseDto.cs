namespace Presentation.Models.Rules;

public record RuleDeploymentResponseDto
{
    public required Guid RuleDeploymentId { get; init; }
    public required Guid RuleId { get; init; }
    public required Guid TargetRuleVersionId { get; init; }
    public required Guid? PreviousRuleVersionId { get; init; }
    public required string Strategy { get; init; }
    public required string Status { get; init; }
    public required int CurrentPercentage { get; init; }
    public required int? StepPercentage { get; init; }
    public required int? StepIntervalSeconds { get; init; }
    public required Guid CreatedByUserId { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
    public required DateTimeOffset? CompletedAt { get; init; }

    public required string Mode { get; init; }
}
