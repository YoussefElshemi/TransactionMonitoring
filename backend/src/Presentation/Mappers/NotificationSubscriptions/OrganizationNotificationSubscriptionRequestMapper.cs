using Core.Models.NotificationSubscriptions;
using Core.ValueObjects;
using Presentation.Models.NotificationSubscriptions;

namespace Presentation.Mappers.NotificationSubscriptions;

public static class OrganizationNotificationSubscriptionRequestMapper
{
    public static ListNotificationSubscriptionRequest ToModel(
        this OrganizationListNotificationSubscriptionRequestDto organizationListNotificationSubscriptionRequestDto)
    {
        return new ListNotificationSubscriptionRequest
        {
            OrganizationId = new OrganizationId(organizationListNotificationSubscriptionRequestDto.OrganizationId),
            Channel = organizationListNotificationSubscriptionRequestDto.Channel,
            IsEnabled = organizationListNotificationSubscriptionRequestDto.IsEnabled.HasValue
                ? new IsEnabled(organizationListNotificationSubscriptionRequestDto.IsEnabled.Value)
                : null,
            PageNumber = new PageNumber(organizationListNotificationSubscriptionRequestDto.PageNumber),
            PageSize = new PageSize(organizationListNotificationSubscriptionRequestDto.PageSize),
            FromDate = new DateRange(organizationListNotificationSubscriptionRequestDto.FromDate),
            ToDate = new DateRange(organizationListNotificationSubscriptionRequestDto.ToDate)
        };
    }

    public static CreateNotificationSubscriptionRequest ToModel(
        this OrganizationCreateNotificationSubscriptionRequestDto organizationCreateNotificationSubscriptionRequestDto)
    {
        var body = organizationCreateNotificationSubscriptionRequestDto.Body;

        return new CreateNotificationSubscriptionRequest
        {
            OrganizationId = new OrganizationId(organizationCreateNotificationSubscriptionRequestDto.OrganizationId),
            Channel = body.Channel,
            Destination = new Destination(body.Destination),
            MinimumSeverity = body.MinimumSeverity,
            NotifyOnBreach = new NotifyOnBreach(body.NotifyOnBreach),
            IsEnabled = new IsEnabled(body.IsEnabled),
            Locale = string.IsNullOrWhiteSpace(body.Locale) ? null : new Locale(body.Locale)
        };
    }

    public static UpdateNotificationSubscriptionRequest ToModel(
        this OrganizationUpdateNotificationSubscriptionRequestDto organizationUpdateNotificationSubscriptionRequestDto)
    {
        var body = organizationUpdateNotificationSubscriptionRequestDto.Body;

        return new UpdateNotificationSubscriptionRequest
        {
            NotificationSubscriptionId = new NotificationSubscriptionId(
                organizationUpdateNotificationSubscriptionRequestDto.NotificationSubscriptionId),
            Destination = string.IsNullOrWhiteSpace(body.Destination) ? null : new Destination(body.Destination),
            MinimumSeverity = body.MinimumSeverity,
            NotifyOnBreach = body.NotifyOnBreach.HasValue ? new NotifyOnBreach(body.NotifyOnBreach.Value) : null,
            IsEnabled = body.IsEnabled.HasValue ? new IsEnabled(body.IsEnabled.Value) : null,
            Locale = body.Locale.ToLocale()
        };
    }
}
