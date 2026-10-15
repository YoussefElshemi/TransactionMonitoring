using Microsoft.AspNetCore.Mvc;

namespace Presentation.Models.Rules;

public record DeployRuleRequestDto : RuleRequestDto
{
    [FromBody]
    public required DeployRuleRequestBodyDto Body { get; init; }
}

public record DeployRuleRequestBodyDto
{
    public required int VersionNumber { get; init; }
    public required string Strategy { get; init; }
    public int? StepPercentage { get; init; }
    public int? StepIntervalSeconds { get; init; }
}
