using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.Rules;

public record CreateRuleVersionRequest
{
    public required RuleId RuleId { get; init; }
    public required RuleBody RuleBody { get; init; }
    public required RuleAction Action { get; init; }
    public required RuleSeverity Severity { get; init; }
    public required RiskWeight RiskWeight { get; init; }
    public required ApplicationUserId CreatedByUserId { get; init; }
}
