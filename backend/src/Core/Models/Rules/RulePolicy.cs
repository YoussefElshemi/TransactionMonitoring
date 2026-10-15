using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.Rules;

public record RulePolicy
{
    public required RuleAction Action { get; init; }
    public required RuleSeverity Severity { get; init; }
    public required RiskWeight RiskWeight { get; init; }
}
