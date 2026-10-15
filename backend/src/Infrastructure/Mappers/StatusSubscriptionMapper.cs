using Core.Models.Status;
using Core.ValueObjects;
using Infrastructure.Entities;

namespace Infrastructure.Mappers;

public static class StatusSubscriptionMapper
{
    public static StatusSubscription ToModel(this StatusSubscriptionEntity statusSubscriptionEntity)
    {
        return new StatusSubscription
        {
            StatusSubscriptionId = new StatusSubscriptionId(statusSubscriptionEntity.StatusSubscriptionId),
            Email = new Email(statusSubscriptionEntity.Email),
            ConfirmedAt = statusSubscriptionEntity.ConfirmedAt.HasValue ? new ConfirmedAt(statusSubscriptionEntity.ConfirmedAt.Value) : null,
            UnsubscribedAt = statusSubscriptionEntity.UnsubscribedAt.HasValue ? new RevokedAt(statusSubscriptionEntity.UnsubscribedAt.Value) : null,
            CreatedAt = new CreatedAt(statusSubscriptionEntity.CreatedAt),
            UpdatedAt = new UpdatedAt(statusSubscriptionEntity.UpdatedAt)
        };
    }
}
