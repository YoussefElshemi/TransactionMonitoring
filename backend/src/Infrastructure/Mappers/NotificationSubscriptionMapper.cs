using Core.Enums;
using Core.Models.NotificationSubscriptions;
using Core.ValueObjects;
using Infrastructure.Entities;

namespace Infrastructure.Mappers;

public static class NotificationSubscriptionMapper
{
    public static NotificationSubscription ToModel(this NotificationSubscriptionEntity notificationSubscriptionEntity)
    {
        return new NotificationSubscription
        {
            Mode = (Mode)notificationSubscriptionEntity.ModeId,
            Locale = notificationSubscriptionEntity.Locale is not null ? new Locale(notificationSubscriptionEntity.Locale) : null,
            NotificationSubscriptionId = new NotificationSubscriptionId(notificationSubscriptionEntity.NotificationSubscriptionId),
            OrganizationId = new OrganizationId(notificationSubscriptionEntity.OrganizationId),
            Channel = (NotificationChannel)notificationSubscriptionEntity.ChannelId,
            Destination = new Destination(notificationSubscriptionEntity.Destination),
            MinimumSeverity = notificationSubscriptionEntity.MinimumSeverityId.HasValue
                ? (RuleSeverity)notificationSubscriptionEntity.MinimumSeverityId.Value
                : null,
            NotifyOnBreach = new NotifyOnBreach(notificationSubscriptionEntity.NotifyOnBreach),
            IsEnabled = new IsEnabled(notificationSubscriptionEntity.IsEnabled),
            CreatedAt = new CreatedAt(notificationSubscriptionEntity.CreatedAt),
            UpdatedAt = new UpdatedAt(notificationSubscriptionEntity.UpdatedAt),
            IsDeleted = new IsDeleted(notificationSubscriptionEntity.IsDeleted),
            DeletedAt = notificationSubscriptionEntity.DeletedAt.HasValue ? new DeletedAt(notificationSubscriptionEntity.DeletedAt.Value) : null
        };
    }

    public static NotificationSubscriptionEntity ToEntity(this NotificationSubscription notificationSubscription)
    {
        return new NotificationSubscriptionEntity
        {
            ModeId = (int)notificationSubscription.Mode,
            Locale = notificationSubscription.Locale?.ToString(),
            NotificationSubscriptionId = notificationSubscription.NotificationSubscriptionId,
            OrganizationId = notificationSubscription.OrganizationId,
            ChannelId = (int)notificationSubscription.Channel,
            Destination = notificationSubscription.Destination,
            MinimumSeverityId = notificationSubscription.MinimumSeverity.HasValue
                ? (int)notificationSubscription.MinimumSeverity.Value
                : null,
            NotifyOnBreach = notificationSubscription.NotifyOnBreach,
            IsEnabled = notificationSubscription.IsEnabled,
            CreatedAt = notificationSubscription.CreatedAt,
            UpdatedAt = notificationSubscription.UpdatedAt,
            IsDeleted = notificationSubscription.IsDeleted,
            DeletedAt = notificationSubscription.DeletedAt
        };
    }
}
