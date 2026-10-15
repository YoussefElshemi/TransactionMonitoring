using Microsoft.AspNetCore.Mvc;
using Presentation.Models.Rules;

namespace Presentation.Models.Queues;

public record UpdateQueueRequestDto : OrganizationQueueRequestDto
{
    [FromBody]
    public required UpdateQueueRequestBodyDto Body { get; init; }
}

public record UpdateQueueRequestBodyDto
{
    public Guid? SubOrganizationId { get; init; }
    public bool? ClearSubOrganization { get; init; }

    public string? Name { get; init; }
    public string? Description { get; init; }
    public int? Priority { get; init; }
    public string? RoutingPolicy { get; init; }
    public string? MinimumSeverity { get; init; }

    public bool? ClearMinimumSeverity { get; init; }
    public Guid[]? RuleIds { get; init; }
    public bool? ClearRuleIds { get; init; }
    public RuleBodyDto? Matcher { get; init; }
    public bool? ClearMatcher { get; init; }
    public bool? IsEnabled { get; init; }
}
