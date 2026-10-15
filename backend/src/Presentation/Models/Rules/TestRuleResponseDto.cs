namespace Presentation.Models.Rules;

public record TestRuleResponseDto
{
    public required bool Matched { get; init; }
    public required RulePolicyResponseDto Policy { get; init; }
    public required RuleOutcomeResponseDto Outcome { get; init; }
}
