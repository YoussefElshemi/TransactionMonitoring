using Core.ValueObjects;

namespace Core.Models.OutboxMessages;

public record WebhookReplayBatch
{
    public required OrganizationId OrganizationId { get; init; }
    public required PeriodStart From { get; init; }
    public required PeriodEnd To { get; init; }
    public required Count Matched { get; init; }
    public required Count Queued { get; init; }
    public required HasMore HasMore { get; init; }
}
