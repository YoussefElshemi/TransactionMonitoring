using Core.Models.NotificationSubscriptions;
using Core.ValueObjects;
using Presentation.Models.NotificationSubscriptions;

namespace Presentation.Mappers.NotificationSubscriptions;

public static class UpdateNotificationSubscriptionRequestMapper
{
    public static UpdateNotificationSubscriptionRequest ToModel(this UpdateNotificationSubscriptionRequestDto updateNotificationSubscriptionRequestDto)
    {
        return new UpdateNotificationSubscriptionRequest
        {
            NotificationSubscriptionId = new NotificationSubscriptionId(updateNotificationSubscriptionRequestDto.NotificationSubscriptionId),
            Destination = string.IsNullOrWhiteSpace(updateNotificationSubscriptionRequestDto.Body.Destination)
                ? null
                : new Destination(updateNotificationSubscriptionRequestDto.Body.Destination),
            MinimumSeverity = updateNotificationSubscriptionRequestDto.Body.MinimumSeverity,
            NotifyOnBreach = updateNotificationSubscriptionRequestDto.Body.NotifyOnBreach.HasValue
                ? new NotifyOnBreach(updateNotificationSubscriptionRequestDto.Body.NotifyOnBreach.Value)
                : null,
            IsEnabled = updateNotificationSubscriptionRequestDto.Body.IsEnabled.HasValue
                ? new IsEnabled(updateNotificationSubscriptionRequestDto.Body.IsEnabled.Value)
                : null,
            Locale = updateNotificationSubscriptionRequestDto.Body.Locale.ToLocale()
        };
    }
}
