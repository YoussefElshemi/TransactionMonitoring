using Core.Models.BaseModels;
using Core.ValueObjects;

namespace Core.Models.Status;

public record StatusSubscription : BaseModel
{
    public required StatusSubscriptionId StatusSubscriptionId { get; init; }
    public required Email Email { get; init; }
    public required ConfirmedAt? ConfirmedAt { get; init; }
    public required RevokedAt? UnsubscribedAt { get; init; }
}
