using Core.ValueObjects;

namespace Core.Models.Status;

public record IssuedStatusSubscription
{
    public required StatusSubscription StatusSubscription { get; init; }
    public required StatusSubscriptionToken ConfirmationToken { get; init; }
    public required StatusSubscriptionToken UnsubscribeToken { get; init; }
}
