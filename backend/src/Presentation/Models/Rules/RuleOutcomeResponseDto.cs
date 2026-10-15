namespace Presentation.Models.Rules;

public record RuleOutcomeResponseDto
{
    public required string Action { get; init; }
    public required string Severity { get; init; }
    public required int RiskScore { get; init; }
}
