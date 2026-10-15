using System.Text.Json.Nodes;
using Core.Enums;

namespace Presentation.Models.Rules;

public record TestRuleRequestDto
{
    public required RuleBodyDto RuleBody { get; init; }
    public required JsonObject Payload { get; init; }
    public string MatchPolarity { get; init; } = nameof(RuleMatchPolarity.MatchesWhenTrue);
    public string Action { get; init; } = nameof(RuleAction.Review);
    public string Severity { get; init; } = nameof(RuleSeverity.Medium);
    public int RiskWeight { get; init; }
}
