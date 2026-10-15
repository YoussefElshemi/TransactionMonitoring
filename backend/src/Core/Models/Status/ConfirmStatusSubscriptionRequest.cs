using Core.ValueObjects;

namespace Core.Models.Status;

public record ConfirmStatusSubscriptionRequest
{
    public required StatusSubscriptionId StatusSubscriptionId { get; init; }
    public required StatusSubscriptionToken Token { get; init; }
}
