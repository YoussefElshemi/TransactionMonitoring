using Core.Enums;
using Core.Models.NotificationSubscriptions;
using Core.ValueObjects;

namespace Core.Mappers;

public static class CreateNotificationSubscriptionRequestMapper
{
    public static NotificationSubscription ToModel(
        this CreateNotificationSubscriptionRequest createNotificationSubscriptionRequest, TimeProvider timeProvider, Mode mode)
    {
        return new NotificationSubscription
        {
            Mode = mode,
            NotificationSubscriptionId = new NotificationSubscriptionId(Guid.NewGuid()),
            OrganizationId = createNotificationSubscriptionRequest.OrganizationId,
            Channel = createNotificationSubscriptionRequest.Channel,
            Destination = createNotificationSubscriptionRequest.Destination,
            MinimumSeverity = createNotificationSubscriptionRequest.MinimumSeverity,
            NotifyOnBreach = createNotificationSubscriptionRequest.NotifyOnBreach,
            IsEnabled = createNotificationSubscriptionRequest.IsEnabled,
            Locale = createNotificationSubscriptionRequest.Locale,
            CreatedAt = new CreatedAt(timeProvider.GetUtcNow()),
            UpdatedAt = new UpdatedAt(timeProvider.GetUtcNow()),
            IsDeleted = new IsDeleted(false),
            DeletedAt = null
        };
    }
}
