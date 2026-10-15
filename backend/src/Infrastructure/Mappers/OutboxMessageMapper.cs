using Core.Enums;
using Core.Models.OutboxMessages;
using Core.ValueObjects;
using Infrastructure.Entities;

namespace Infrastructure.Mappers;

public static class OutboxMessageMapper
{
    public static OutboxMessage ToModel(this OutboxMessageEntity outboxMessageEntity)
    {
        return new OutboxMessage
        {
            Mode = (Mode)outboxMessageEntity.ModeId,
            OutboxMessageId = new OutboxMessageId(outboxMessageEntity.OutboxMessageId),
            OrganizationId = outboxMessageEntity.OrganizationId.HasValue ? new OrganizationId(outboxMessageEntity.OrganizationId.Value) : null,
            Payload = new Payload(outboxMessageEntity.Payload),
            Channel = (NotificationChannel)outboxMessageEntity.ChannelId,
            Destination = new Destination(outboxMessageEntity.Destination),
            SecretKey = outboxMessageEntity.SecretKey is null ? null : new SecretKey(outboxMessageEntity.SecretKey),
            WebhookConfigurationId = outboxMessageEntity.WebhookConfigurationId.HasValue
                ? new WebhookConfigurationId(outboxMessageEntity.WebhookConfigurationId.Value)
                : null,
            Event = outboxMessageEntity.Event is null ? null : new WebhookEvent(outboxMessageEntity.Event),
            PayloadVersion = outboxMessageEntity.PayloadVersion.HasValue
                ? new WebhookPayloadVersion(outboxMessageEntity.PayloadVersion.Value)
                : null,
            ReplayOfOutboxMessageId = outboxMessageEntity.ReplayOfOutboxMessageId.HasValue
                ? new OutboxMessageId(outboxMessageEntity.ReplayOfOutboxMessageId.Value)
                : null,
            Attempts = new Attempts(outboxMessageEntity.Attempts),
            LastAttemptedAt = outboxMessageEntity.LastAttemptedAt.HasValue ? new LastAttemptedAt(outboxMessageEntity.LastAttemptedAt.Value) : null,
            ResponseStatusCode = outboxMessageEntity.ResponseStatusCode.HasValue ? new StatusCode(outboxMessageEntity.ResponseStatusCode.Value) : null,
            ResponseBody = outboxMessageEntity.ResponseBody is null ? null : new ResponseBody(outboxMessageEntity.ResponseBody),
            Latency = outboxMessageEntity.Latency.HasValue ? new LatencyMilliseconds(outboxMessageEntity.Latency.Value) : null,
            SuppressedAt = outboxMessageEntity.SuppressedAt.HasValue ? new SuppressedAt(outboxMessageEntity.SuppressedAt.Value) : null,
            IsProcessed = new IsProcessed(outboxMessageEntity.IsProcessed),
            ProcessedAt = outboxMessageEntity.ProcessedAt.HasValue ? new ProcessedAt(outboxMessageEntity.ProcessedAt.Value) : null,
            CreatedAt = new CreatedAt(outboxMessageEntity.CreatedAt),
            UpdatedAt = new UpdatedAt(outboxMessageEntity.UpdatedAt),
            IsDeleted = new IsDeleted(outboxMessageEntity.IsDeleted),
            DeletedAt = outboxMessageEntity.DeletedAt.HasValue ? new DeletedAt(outboxMessageEntity.DeletedAt.Value) : null
        };
    }

    public static OutboxMessageEntity ToEntity(this OutboxMessage outboxMessage)
    {
        return new OutboxMessageEntity
        {
            ModeId = (int)outboxMessage.Mode,
            OutboxMessageId = outboxMessage.OutboxMessageId,
            OrganizationId = outboxMessage.OrganizationId,
            Payload = outboxMessage.Payload,
            ChannelId = (int)outboxMessage.Channel,
            Destination = outboxMessage.Destination,
            SecretKey = outboxMessage.SecretKey?.ToString(),
            WebhookConfigurationId = outboxMessage.WebhookConfigurationId,
            Event = outboxMessage.Event?.ToString(),
            PayloadVersion = outboxMessage.PayloadVersion,
            ReplayOfOutboxMessageId = outboxMessage.ReplayOfOutboxMessageId,
            Attempts = outboxMessage.Attempts,
            LastAttemptedAt = outboxMessage.LastAttemptedAt,
            ResponseStatusCode = outboxMessage.ResponseStatusCode,
            ResponseBody = outboxMessage.ResponseBody?.ToString(),
            Latency = outboxMessage.Latency,
            SuppressedAt = outboxMessage.SuppressedAt,
            IsProcessed = outboxMessage.IsProcessed,
            ProcessedAt = outboxMessage.ProcessedAt,
            CreatedAt = outboxMessage.CreatedAt,
            UpdatedAt = outboxMessage.UpdatedAt,
            IsDeleted = outboxMessage.IsDeleted,
            DeletedAt = outboxMessage.DeletedAt
        };
    }
}
