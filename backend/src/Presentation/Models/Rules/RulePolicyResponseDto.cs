namespace Presentation.Models.Rules;

public record RulePolicyResponseDto
{
    public required string Action { get; init; }
    public required string Severity { get; init; }
    public required int RiskWeight { get; init; }
}
