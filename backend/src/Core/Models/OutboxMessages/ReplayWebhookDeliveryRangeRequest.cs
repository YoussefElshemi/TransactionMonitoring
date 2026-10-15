using Core.ValueObjects;

namespace Core.Models.OutboxMessages;

public record ReplayWebhookDeliveryRangeRequest
{
    public required OrganizationId OrganizationId { get; init; }
    public required PeriodStart From { get; init; }
    public required PeriodEnd To { get; init; }

    public required WebhookConfigurationId? WebhookConfigurationId { get; init; }
    public required WebhookEvent? Event { get; init; }

    public required IncludeDelivered IncludeDelivered { get; init; }
}
