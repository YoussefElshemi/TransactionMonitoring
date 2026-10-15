using Core.Enums;
using Core.Models.NotificationSubscriptions;

namespace Core.Helpers;

public static class NotificationSubscriptionMatcher
{
    public static List<NotificationSubscription> ForSeverity(
        this List<NotificationSubscription> notificationSubscriptions, RuleSeverity severity)
    {
        return notificationSubscriptions
            .Where(x => !x.MinimumSeverity.HasValue || severity >= x.MinimumSeverity.Value)
            .ToList();
    }

    public static List<NotificationSubscription> ForBreach(this List<NotificationSubscription> notificationSubscriptions)
    {
        return notificationSubscriptions
            .Where(x => x.NotifyOnBreach)
            .ToList();
    }
}
