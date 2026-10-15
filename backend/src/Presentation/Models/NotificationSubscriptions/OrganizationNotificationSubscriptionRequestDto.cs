using Core.Enums;
using Microsoft.AspNetCore.Mvc;
using Presentation.Models.Common;
using Presentation.Models.Organizations;

namespace Presentation.Models.NotificationSubscriptions;

public record OrganizationNotificationSubscriptionRequestDto : OrganizationRequestDto
{
    [FromRoute]
    public required Guid NotificationSubscriptionId { get; init; }
}

public record OrganizationCreateNotificationSubscriptionRequestDto : OrganizationRequestDto
{
    [FromBody]
    public required CreateNotificationSubscriptionRequestBodyDto Body { get; init; }
}

public record OrganizationUpdateNotificationSubscriptionRequestDto : OrganizationNotificationSubscriptionRequestDto
{
    [FromBody]
    public required UpdateNotificationSubscriptionRequestBodyDto Body { get; init; }
}

public record OrganizationListNotificationSubscriptionRequestDto : PagedRequestDto
{
    [FromRoute]
    public new required Guid OrganizationId
    {
        get => base.OrganizationId;
        init => base.OrganizationId = value;
    }

    [FromQuery]
    public NotificationChannel? Channel { get; init; }

    [FromQuery]
    public bool? IsEnabled { get; init; }
}
