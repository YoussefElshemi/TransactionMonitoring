using System.Text.Json.Serialization;
using Core.Enums;
using Core.Models.Common;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Models.NotificationSubscriptions;

public record UpdateNotificationSubscriptionRequestDto : NotificationSubscriptionRequestDto
{
    [FromBody]
    public required UpdateNotificationSubscriptionRequestBodyDto Body { get; init; }
}

public record UpdateNotificationSubscriptionRequestBodyDto
{
    public string? Destination { get; init; }
    public RuleSeverity? MinimumSeverity { get; init; }
    public bool? NotifyOnBreach { get; init; }
    public bool? IsEnabled { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Patch<string?> Locale { get; init; }
}
