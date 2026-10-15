namespace Presentation.Models.Rules;

public record RuleResponseDto
{
    public required Guid RuleId { get; init; }
    public required string FriendlyName { get; init; }
    public required RuleBodyDto RuleBody { get; init; }
    public required string MatchPolarity { get; init; }
    public required string Action { get; init; }
    public required string Severity { get; init; }
    public required int RiskWeight { get; init; }
    public required int? HoldWindow { get; init; }
    public required string? HoldExpiryVerdict { get; init; }
    public required Guid OrganizationId { get; init; }
    public required bool IsEnabled { get; init; }
    public required Guid RuleSetId { get; init; }
    public required Guid CreatedByUserId { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
    public required bool IsDeleted { get; init; }

    public required string Mode { get; init; }
}
