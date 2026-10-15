using Microsoft.AspNetCore.Mvc;

namespace Presentation.Models.RuleSets;

public record UpdateRuleSetRequestDto : RuleSetRequestDto
{
    [FromBody]
    public required UpdateRuleSetRequestBodyDto Body { get; init; }
}

public record UpdateRuleSetRequestBodyDto
{
    public Guid? SubOrganizationId { get; init; }
    public bool? ClearSubOrganization { get; init; }

    public string? FriendlyName { get; init; }
    public bool? IsEnabled { get; init; }
    public int? ReviewThreshold { get; init; }
}