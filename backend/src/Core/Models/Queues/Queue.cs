using Core.Enums;
using Core.Models.BaseModels;
using Core.Models.Rules;
using Core.ValueObjects;

namespace Core.Models.Queues;

public record Queue : DeletableModel
{
    public required QueueId QueueId { get; init; }
    public required OrganizationId OrganizationId { get; init; }

    public required SubOrganizationId? SubOrganizationId { get; init; }
    public required QueueName Name { get; init; }
    public required QueueDescription? Description { get; init; }
    public required QueuePriority Priority { get; init; }
    public required QueueRoutingPolicy RoutingPolicy { get; init; }

    public required RuleSeverity? MinimumSeverity { get; init; }

    public required List<RuleId> RuleIds { get; init; }

    public required RuleBody? Matcher { get; init; }

    public required IsEnabled IsEnabled { get; init; }
    public required List<QueueMember> Members { get; init; }

    public required Mode Mode { get; init; }
}
