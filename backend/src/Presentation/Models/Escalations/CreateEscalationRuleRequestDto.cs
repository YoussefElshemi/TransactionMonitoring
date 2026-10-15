using Microsoft.AspNetCore.Mvc;
using Presentation.Models.Rules;

namespace Presentation.Models.Escalations;

public record CreateEscalationRuleRequestDto
{
    [FromRoute]
    public required Guid OrganizationId { get; init; }

    [FromBody]
    public required CreateEscalationRuleRequestBodyDto Body { get; init; }
}

public record CreateEscalationRuleRequestBodyDto
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public int? Priority { get; init; }
    public required bool IsEnabled { get; init; }

    public int? UnassignedForMinutes { get; init; }
    public int? HoldExpiryWithinMinutes { get; init; }
    public string? MinimumSeverity { get; init; }

    public RuleBodyDto? Matcher { get; init; }

    public Guid? TargetQueueId { get; init; }
    public string? RaiseSeverityTo { get; init; }
}
