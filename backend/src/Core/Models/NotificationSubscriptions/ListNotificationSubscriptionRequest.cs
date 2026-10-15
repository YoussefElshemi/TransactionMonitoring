using Core.Enums;
using Core.Models.Common;
using Core.ValueObjects;

namespace Core.Models.NotificationSubscriptions;

public record ListNotificationSubscriptionRequest : PagedRequest
{
    public required OrganizationId OrganizationId { get; init; }
    public required NotificationChannel? Channel { get; init; }
    public required IsEnabled? IsEnabled { get; init; }
}
