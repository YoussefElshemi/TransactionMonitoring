using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.Rules;

public record RuleOutcome
{
    public required RuleAction Action { get; init; }
    public required RuleSeverity Severity { get; init; }
    public required RiskScore RiskScore { get; init; }
}
