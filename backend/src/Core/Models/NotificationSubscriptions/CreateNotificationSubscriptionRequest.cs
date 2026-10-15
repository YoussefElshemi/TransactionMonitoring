using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.NotificationSubscriptions;

public record CreateNotificationSubscriptionRequest
{
    public required OrganizationId OrganizationId { get; init; }
    public required NotificationChannel Channel { get; init; }
    public required Destination Destination { get; init; }
    public required RuleSeverity? MinimumSeverity { get; init; }
    public required NotifyOnBreach NotifyOnBreach { get; init; }
    public required IsEnabled IsEnabled { get; init; }
    public required Locale? Locale { get; init; }
}
