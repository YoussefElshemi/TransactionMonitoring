namespace Presentation.Models.Rules;

public record RuleVersionResponseDto
{
    public required Guid RuleVersionId { get; init; }
    public required Guid RuleId { get; init; }
    public required int VersionNumber { get; init; }
    public required RuleBodyDto RuleBody { get; init; }
    public required string MatchPolarity { get; init; }
    public required string Action { get; init; }
    public required string Severity { get; init; }
    public required int RiskWeight { get; init; }
    public required Guid RuleSetId { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }

    public required string Mode { get; init; }
}
