using Microsoft.AspNetCore.Mvc;
using Presentation.Models.Rules;

namespace Presentation.Models.Escalations;

public record UpdateEscalationRuleRequestDto : OrganizationEscalationRuleRequestDto
{
    [FromBody]
    public required UpdateEscalationRuleRequestBodyDto Body { get; init; }
}

public record UpdateEscalationRuleRequestBodyDto
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public int? Priority { get; init; }
    public bool? IsEnabled { get; init; }

    public int? UnassignedForMinutes { get; init; }
    public bool? ClearUnassignedFor { get; init; }
    public int? HoldExpiryWithinMinutes { get; init; }
    public bool? ClearHoldExpiryWithin { get; init; }
    public string? MinimumSeverity { get; init; }
    public bool? ClearMinimumSeverity { get; init; }
    public RuleBodyDto? Matcher { get; init; }
    public bool? ClearMatcher { get; init; }
    public Guid? TargetQueueId { get; init; }
    public bool? ClearTargetQueueId { get; init; }
    public string? RaiseSeverityTo { get; init; }
    public bool? ClearRaiseSeverityTo { get; init; }
}
