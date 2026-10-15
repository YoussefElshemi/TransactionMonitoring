using Presentation.Models.Rules;

namespace Presentation.Models.Escalations;

public record EscalationRuleResponseDto
{
    public required Guid EscalationRuleId { get; init; }
    public required Guid OrganizationId { get; init; }
    public required string Name { get; init; }
    public required string? Description { get; init; }
    public required int Priority { get; init; }
    public required bool IsEnabled { get; init; }
    public required int? UnassignedForMinutes { get; init; }
    public required int? HoldExpiryWithinMinutes { get; init; }
    public required string? MinimumSeverity { get; init; }
    public required RuleBodyDto? Matcher { get; init; }
    public required Guid? TargetQueueId { get; init; }
    public required string? RaiseSeverityTo { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
    public required bool IsDeleted { get; init; }
    public required DateTimeOffset? DeletedAt { get; init; }

    public required string Mode { get; init; }
}
