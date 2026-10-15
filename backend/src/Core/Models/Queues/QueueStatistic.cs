using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.Queues;

public record QueueStatistic
{
    public required QueueId QueueId { get; init; }
    public required QueueName Name { get; init; }
    public required QueuePriority Priority { get; init; }
    public required QueueRoutingPolicy RoutingPolicy { get; init; }
    public required IsEnabled IsEnabled { get; init; }

    public required TotalCount Depth { get; init; }
    public required TotalCount InProgressCount { get; init; }

    public required TimeSpan? OldestWaitingFor { get; init; }
    public required List<QueueMemberLoad> MemberLoads { get; init; }
}
