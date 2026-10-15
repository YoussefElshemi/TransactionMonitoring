using Microsoft.AspNetCore.Mvc;
using Presentation.Models.Rules;

namespace Presentation.Models.Queues;

public record CreateQueueRequestDto
{
    [FromRoute]
    public required Guid OrganizationId { get; init; }

    [FromBody]
    public required CreateQueueRequestBodyDto Body { get; init; }
}

public record CreateQueueRequestBodyDto
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public int? Priority { get; init; }
    public required string RoutingPolicy { get; init; }
    public string? MinimumSeverity { get; init; }
    public Guid[]? RuleIds { get; init; }

    public RuleBodyDto? Matcher { get; init; }

    public required bool IsEnabled { get; init; }
    public Guid[]? MemberUserIds { get; init; }

    public Guid? SubOrganizationId { get; init; }
}
