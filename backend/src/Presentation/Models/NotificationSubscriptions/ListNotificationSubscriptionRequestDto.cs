using Core.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Presentation.Models.Common;
using Swashbuckle.AspNetCore.Annotations;

namespace Presentation.Models.NotificationSubscriptions;

public record ListNotificationSubscriptionRequestDto : PagedRequestDto
{
    [FromQuery]
    public NotificationChannel? Channel { get; init; }

    [FromQuery]
    public bool? IsEnabled { get; init; }

    [BindNever]
    [SwaggerSchema(ReadOnly = true)]
    public new required Guid OrganizationId
    {
        get => base.OrganizationId;
        set => base.OrganizationId = value;
    }
}
