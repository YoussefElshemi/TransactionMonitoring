using Core.Models.NotificationSubscriptions;
using Presentation.Mappers.NotificationSubscriptions;
using Presentation.Models.NotificationSubscriptions;

namespace Presentation.Factories;

public class NotificationSubscriptionResponseFactory
{
    public NotificationSubscriptionResourceDto Create(NotificationSubscription notificationSubscription)
    {
        return new NotificationSubscriptionResourceDto
        {
            NotificationSubscription = notificationSubscription.ToDto()
        };
    }

    public NotificationSubscriptionResourceDto[] Create(IReadOnlyCollection<NotificationSubscription> notificationSubscriptions)
    {
        return notificationSubscriptions.Select(Create).ToArray();
    }
}
