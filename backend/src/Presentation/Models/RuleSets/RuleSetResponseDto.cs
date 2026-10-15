namespace Presentation.Models.RuleSets;

public record RuleSetResponseDto
{
    public required Guid RuleSetId { get; init; }
    public required string FriendlyName { get; init; }
    public required int RuleCount { get; init; }
    public required Guid OrganizationId { get; init; }
    public required bool IsEnabled { get; init; }
    public required int? ReviewThreshold { get; init; }
    public required Guid CreatedByUserId { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }

    public required string Mode { get; init; }
}
