using Core.Enums;
using Core.Models.Common;
using Core.ValueObjects;

namespace Core.Models.NotificationSubscriptions;

public record UpdateNotificationSubscriptionRequest
{
    public required NotificationSubscriptionId NotificationSubscriptionId { get; init; }
    public required Destination? Destination { get; init; }
    public required RuleSeverity? MinimumSeverity { get; init; }
    public required NotifyOnBreach? NotifyOnBreach { get; init; }
    public required IsEnabled? IsEnabled { get; init; }

    public required Patch<Locale?> Locale { get; init; }
}
