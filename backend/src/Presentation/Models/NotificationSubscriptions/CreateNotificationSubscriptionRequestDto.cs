using Core.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Swashbuckle.AspNetCore.Annotations;

namespace Presentation.Models.NotificationSubscriptions;

public record CreateNotificationSubscriptionRequestDto
{
    [FromBody]
    public required CreateNotificationSubscriptionRequestBodyDto Body { get; init; }

    [BindNever]
    [SwaggerSchema(ReadOnly = true)]
    public Guid OrganizationId { get; set; }
}

public record CreateNotificationSubscriptionRequestBodyDto
{
    public required NotificationChannel Channel { get; init; }
    public required string Destination { get; init; }
    public RuleSeverity? MinimumSeverity { get; init; }
    public bool NotifyOnBreach { get; init; }
    public bool IsEnabled { get; init; } = true;

    public string? Locale { get; init; }
}
