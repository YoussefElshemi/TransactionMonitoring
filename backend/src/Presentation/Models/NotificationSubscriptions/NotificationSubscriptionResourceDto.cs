namespace Presentation.Models.NotificationSubscriptions;

public record NotificationSubscriptionResourceDto
{
    public required NotificationSubscriptionResponseDto NotificationSubscription { get; init; }
}
