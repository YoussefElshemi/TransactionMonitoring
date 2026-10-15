namespace Presentation.Models.Queues;

public record QueueStatisticResponseDto
{
    public required Guid QueueId { get; init; }
    public required string Name { get; init; }
    public required int Priority { get; init; }
    public required string RoutingPolicy { get; init; }
    public required bool IsEnabled { get; init; }
    public required int Depth { get; init; }
    public required int InProgressCount { get; init; }

    public required double? OldestWaitingForSeconds { get; init; }
    public required QueueMemberLoadResponseDto[] MemberLoads { get; init; }
}
