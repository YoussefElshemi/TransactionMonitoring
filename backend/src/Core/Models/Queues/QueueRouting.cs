using Core.ValueObjects;

namespace Core.Models.Queues;

public record QueueRouting
{
    public required QueueId? QueueId { get; init; }

    public required ApplicationUserId? AssignedToUserId { get; init; }
}
