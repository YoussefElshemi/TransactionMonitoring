using Core.Models.NotificationSubscriptions;
using Core.ValueObjects;
using Presentation.Models.NotificationSubscriptions;

namespace Presentation.Mappers.NotificationSubscriptions;

public static class ListNotificationSubscriptionRequestMapper
{
    public static ListNotificationSubscriptionRequest ToModel(this ListNotificationSubscriptionRequestDto listNotificationSubscriptionRequestDto)
    {
        return new ListNotificationSubscriptionRequest
        {
            OrganizationId = new OrganizationId(listNotificationSubscriptionRequestDto.OrganizationId),
            Channel = listNotificationSubscriptionRequestDto.Channel,
            IsEnabled = listNotificationSubscriptionRequestDto.IsEnabled.HasValue
                ? new IsEnabled(listNotificationSubscriptionRequestDto.IsEnabled.Value)
                : null,
            PageNumber = new PageNumber(listNotificationSubscriptionRequestDto.PageNumber),
            PageSize = new PageSize(listNotificationSubscriptionRequestDto.PageSize),
            FromDate = new DateRange(listNotificationSubscriptionRequestDto.FromDate),
            ToDate = new DateRange(listNotificationSubscriptionRequestDto.ToDate)
        };
    }
}
