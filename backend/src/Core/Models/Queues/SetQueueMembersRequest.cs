using Core.ValueObjects;

namespace Core.Models.Queues;

public record SetQueueMembersRequest
{
    public required QueueId QueueId { get; init; }
    public required List<ApplicationUserId> UserIds { get; init; }
}
