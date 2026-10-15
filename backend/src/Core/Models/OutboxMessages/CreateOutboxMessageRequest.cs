using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.OutboxMessages;

public record CreateOutboxMessageRequest
{
    public required OrganizationId OrganizationId { get; init; }
    public required Payload Payload { get; init; }
    public required NotificationChannel Channel { get; init; }
    public required Destination Destination { get; init; }
    public required SecretKey? SecretKey { get; init; }

    public WebhookConfigurationId? WebhookConfigurationId { get; init; }
    public WebhookEvent? Event { get; init; }
    public WebhookPayloadVersion? PayloadVersion { get; init; }
    public OutboxMessageId? ReplayOfOutboxMessageId { get; init; }
}
