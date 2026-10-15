using Core.ValueObjects;

namespace Core.Models.Queues;

public record ClaimNextRequest
{
    public required OrganizationId OrganizationId { get; init; }
    public required ApplicationUserId UserId { get; init; }

    public required QueueId? QueueId { get; init; }
}
