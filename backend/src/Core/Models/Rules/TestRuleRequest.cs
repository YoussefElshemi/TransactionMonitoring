using System.Text.Json.Nodes;
using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.Rules;

public record TestRuleRequest
{
    public required RuleBody RuleBody { get; init; }
    public required JsonObject Payload { get; init; }
    public required RuleMatchPolarity MatchPolarity { get; init; }
    public required RuleAction Action { get; init; }
    public required RuleSeverity Severity { get; init; }
    public required RiskWeight RiskWeight { get; init; }
    public required OrganizationId OrganizationId { get; init; }
}
