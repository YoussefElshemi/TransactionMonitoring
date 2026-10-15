using Core.Models.NotificationSubscriptions;
using Presentation.Models.NotificationSubscriptions;

namespace Presentation.Mappers.NotificationSubscriptions;

public static class NotificationSubscriptionDtoMapper
{
    public static NotificationSubscriptionResponseDto ToDto(this NotificationSubscription notificationSubscription)
    {
        return new NotificationSubscriptionResponseDto
        {
            Mode = notificationSubscription.Mode.ToString(),
            Locale = notificationSubscription.Locale?.ToString(),
            NotificationSubscriptionId = notificationSubscription.NotificationSubscriptionId,
            OrganizationId = notificationSubscription.OrganizationId,
            Channel = notificationSubscription.Channel.ToString(),
            Destination = notificationSubscription.Destination,
            MinimumSeverity = notificationSubscription.MinimumSeverity?.ToString(),
            NotifyOnBreach = notificationSubscription.NotifyOnBreach,
            IsEnabled = notificationSubscription.IsEnabled,
            CreatedAt = notificationSubscription.CreatedAt,
            UpdatedAt = notificationSubscription.UpdatedAt
        };
    }
}
