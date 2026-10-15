using Core.Enums;
using Core.Models.BaseModels;
using Core.ValueObjects;

namespace Core.Models.OutboxMessages;

public record OutboxMessage : ProcessableModel
{
    public required OutboxMessageId OutboxMessageId { get; init; }

    public required OrganizationId? OrganizationId { get; init; }
    public required Payload Payload { get; init; }
    public required NotificationChannel Channel { get; init; }

    public required Destination Destination { get; init; }

    public required SecretKey? SecretKey { get; init; }

    public required WebhookConfigurationId? WebhookConfigurationId { get; init; }
    public required WebhookEvent? Event { get; init; }

    public required WebhookPayloadVersion? PayloadVersion { get; init; }

    public required Attempts Attempts { get; init; }

    public required LastAttemptedAt? LastAttemptedAt { get; init; }
    public required StatusCode? ResponseStatusCode { get; init; }
    public required ResponseBody? ResponseBody { get; init; }
    public required LatencyMilliseconds? Latency { get; init; }

    public required SuppressedAt? SuppressedAt { get; init; }

    public required OutboxMessageId? ReplayOfOutboxMessageId { get; init; }

    public required Mode Mode { get; init; }
}
