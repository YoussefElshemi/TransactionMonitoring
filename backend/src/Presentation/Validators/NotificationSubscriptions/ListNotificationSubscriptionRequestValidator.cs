using Core.Interfaces.Services;
using Core.ValueObjects;
using Presentation.Mappers.NotificationSubscriptions;
using Presentation.Models.NotificationSubscriptions;
using Presentation.Validators.Common;

namespace Presentation.Validators.NotificationSubscriptions;

public class ListNotificationSubscriptionRequestValidator(
    INotificationSubscriptionService notificationSubscriptionService,
    TimeProvider timeProvider) : PagedRequestValidator<ListNotificationSubscriptionRequestDto>(timeProvider)
{
    protected override Task<TotalCount> GetCountAsync(ListNotificationSubscriptionRequestDto request, CancellationToken stoppingToken)
    {
        return notificationSubscriptionService.GetCountAsync(request.ToModel(), stoppingToken);
    }
}
