using Core.Enums;
using Core.Models.Rules;
using Core.ValueObjects;

namespace Core.Models.Queues;

public record UpdateQueueRequest
{
    public required QueueId QueueId { get; init; }
    public required QueueName? Name { get; init; }
    public required QueueDescription? Description { get; init; }
    public required QueuePriority? Priority { get; init; }
    public required QueueRoutingPolicy? RoutingPolicy { get; init; }
    public required RuleSeverity? MinimumSeverity { get; init; }

    public required ClearCriteria ClearMinimumSeverity { get; init; }
    public required List<RuleId>? RuleIds { get; init; }
    public required ClearCriteria ClearRuleIds { get; init; }
    public required RuleBody? Matcher { get; init; }
    public required ClearCriteria ClearMatcher { get; init; }
    public required IsEnabled? IsEnabled { get; init; }
    public required SubOrganizationId? SubOrganizationId { get; init; }
    public required ClearCriteria ClearSubOrganization { get; init; }
}
