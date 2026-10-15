using System.Text.Json.Serialization;
using Presentation.Models.Organizations;

namespace Presentation.Models.Webhooks;

public record WebhookDeliveryResponseDto
{
    public required Guid OutboxMessageId { get; init; }
    public required Guid? OrganizationId { get; init; }
    public required Guid? WebhookConfigurationId { get; init; }
    public required string? Event { get; init; }
    public required int? PayloadVersion { get; init; }
    public required string Destination { get; init; }

    public required string Payload { get; init; }
    public required int Attempts { get; init; }

    public required DateTimeOffset? LastAttemptedAt { get; init; }
    public required int? ResponseStatusCode { get; init; }
    public required string? ResponseBody { get; init; }
    public required int? LatencyMilliseconds { get; init; }

    public required bool IsDelivered { get; init; }
    public required DateTimeOffset? DeliveredAt { get; init; }
    public required DateTimeOffset? SuppressedAt { get; init; }

    public required Guid? ReplayOfOutboxMessageId { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }

    public required string Mode { get; init; }
}

public record WebhookDeliveryResourceDto
{
    public required WebhookDeliveryResponseDto WebhookDelivery { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OrganizationResponseDto? Organization { get; init; }
}

public record WebhookReplayBatchResponseDto
{
    public required Guid OrganizationId { get; init; }
    public required DateTimeOffset From { get; init; }
    public required DateTimeOffset To { get; init; }
    public required int Matched { get; init; }
    public required int Queued { get; init; }

    public required bool HasMore { get; init; }
}
