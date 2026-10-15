using Core.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Models.Rules;

public record CreateRuleVersionRequestDto : RuleRequestDto
{
    [FromBody]
    public required CreateRuleVersionRequestBodyDto Body { get; init; }
}

public record CreateRuleVersionRequestBodyDto
{
    public required RuleBodyDto RuleBody { get; init; }
    public string Action { get; init; } = nameof(RuleAction.Review);
    public string Severity { get; init; } = nameof(RuleSeverity.Medium);
    public int RiskWeight { get; init; }
}
