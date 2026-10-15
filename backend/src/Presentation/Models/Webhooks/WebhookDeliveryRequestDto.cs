using Microsoft.AspNetCore.Mvc;
using Presentation.Models.Common;
using Presentation.Models.Organizations;

namespace Presentation.Models.Webhooks;

public record ReplayWebhookDeliveryRequestDto : OrganizationRequestDto
{
    [FromRoute]
    public required Guid OutboxMessageId { get; init; }
}

public record ListWebhookDeliveryRequestDto : PagedRequestDto
{
    [FromRoute]
    public new required Guid OrganizationId
    {
        get => base.OrganizationId;
        init => base.OrganizationId = value;
    }

    [FromQuery]
    public Guid? WebhookConfigurationId { get; init; }

    [FromQuery]
    public string? Event { get; init; }

    [FromQuery]
    public bool? IsDelivered { get; init; }
}

public record ListMyWebhookDeliveryRequestDto : PagedRequestDto
{
    [FromQuery]
    public Guid? WebhookConfigurationId { get; init; }

    [FromQuery]
    public string? Event { get; init; }

    [FromQuery]
    public bool? IsDelivered { get; init; }
}

public record ReplayWebhookDeliveryRangeRequestDto
{
    [FromRoute]
    public required Guid OrganizationId { get; init; }

    [FromBody]
    public required ReplayWebhookDeliveryRangeRequestBodyDto Body { get; init; }
}

public record ReplayWebhookDeliveryRangeRequestBodyDto
{
    public required DateTimeOffset From { get; init; }
    public required DateTimeOffset To { get; init; }
    public Guid? WebhookConfigurationId { get; init; }
    public string? Event { get; init; }

    public bool IncludeDelivered { get; init; }
}
