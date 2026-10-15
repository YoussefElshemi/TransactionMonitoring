using Core.ValueObjects;

namespace Core.Models.Rules;

public record TestRuleResponse
{
    public required Matched Matched { get; init; }
    public required RulePolicy Policy { get; init; }
    public required RuleOutcome Outcome { get; init; }
}
