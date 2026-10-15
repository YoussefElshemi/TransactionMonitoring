using Core.Models.Common;
using Core.ValueObjects;

namespace Core.Models.OutboxMessages;

public record ListWebhookDeliveryRequest : PagedRequest
{
    public required OrganizationId OrganizationId { get; init; }

    public required WebhookConfigurationId? WebhookConfigurationId { get; init; }
    public required WebhookEvent? Event { get; init; }
    public required IsDelivered? IsDelivered { get; init; }
}
