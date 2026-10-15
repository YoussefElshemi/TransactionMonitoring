using Core.ValueObjects;

namespace Core.Models.Status;

public record UnsubscribeStatusSubscriptionRequest
{
    public required StatusSubscriptionId StatusSubscriptionId { get; init; }
    public required StatusSubscriptionToken Token { get; init; }
}
