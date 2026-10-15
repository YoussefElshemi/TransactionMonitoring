using Microsoft.AspNetCore.Mvc;

namespace Presentation.Models.Rules;

public record TransferRuleRequestDto : RuleRequestDto
{
    [FromBody]
    public required TransferRuleRequestBodyDto Body { get; init; }
}

public record TransferRuleRequestBodyDto
{
    public required Guid TargetRuleSetId { get; init; }
}