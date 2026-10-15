using Core.ValueObjects;

namespace Core.Models.OutboxMessages;

public record WebhookDeliveryTally
{
    public required Count Delivered { get; init; }
    public required Count Failed { get; init; }
    public required Count Pending { get; init; }
}
