using Core.Enums;
using Core.Models.OutboxMessages;
using Core.ValueObjects;

namespace Core.Mappers;

public static class CreateOutboxMessageRequestMapper
{
    public static OutboxMessage ToModel(this CreateOutboxMessageRequest createOutboxMessageRequest, TimeProvider timeProvider, Mode mode)
    {
        return new OutboxMessage
        {
            Mode = mode,
            SuppressedAt = null,
            OrganizationId = createOutboxMessageRequest.OrganizationId,
            OutboxMessageId = new OutboxMessageId(Guid.NewGuid()),
            Payload = createOutboxMessageRequest.Payload,
            Channel = createOutboxMessageRequest.Channel,
            Destination = createOutboxMessageRequest.Destination,
            SecretKey = createOutboxMessageRequest.SecretKey,
            WebhookConfigurationId = createOutboxMessageRequest.WebhookConfigurationId,
            Event = createOutboxMessageRequest.Event,
            PayloadVersion = createOutboxMessageRequest.PayloadVersion,
            ReplayOfOutboxMessageId = createOutboxMessageRequest.ReplayOfOutboxMessageId,
            Attempts = new Attempts(0),
            LastAttemptedAt = null,
            ResponseStatusCode = null,
            ResponseBody = null,
            Latency = null,
            IsProcessed = new IsProcessed(false),
            ProcessedAt = null,
            CreatedAt = new CreatedAt(timeProvider.GetUtcNow()),
            UpdatedAt = new UpdatedAt(timeProvider.GetUtcNow()),
            IsDeleted = new IsDeleted(false),
            DeletedAt = null
        };
    }
}
