using AutoFixture;
using Core.Constants;
using Core.Enums;
using Core.Models.OutboxMessages;
using Core.ValueObjects;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.OutboxMessages;

public static class FakeOutboxMessage
{
    public static OutboxMessage CreateValid(IFixture fixture)
    {
        return new OutboxMessage
        {
            Mode = Mode.Live,
            OrganizationId = FakeOrganizationId.CreateValid(fixture),
            SuppressedAt = null,
            OutboxMessageId = FakeOutboxMessageId.CreateValid(fixture),
            Payload = FakePayload.CreateValid(fixture),
            Channel = NotificationChannel.Webhook,
            Destination = FakeDestination.CreateValid(fixture),
            SecretKey = FakeSecretKey.CreateValid(fixture),
            WebhookConfigurationId = FakeWebhookConfigurationId.CreateValid(fixture),
            Event = new WebhookEvent(WebhookEvents.CaseCreated),
            PayloadVersion = new WebhookPayloadVersion(WebhookPayloadVersions.Current),
            ReplayOfOutboxMessageId = null,
            LastAttemptedAt = null,
            ResponseStatusCode = null,
            ResponseBody = null,
            Latency = null,
            Attempts = FakeAttempts.CreateValid(fixture),
            IsProcessed = FakeIsProcessed.CreateValid(fixture),
            ProcessedAt = FakeProcessedAt.CreateValid(fixture),
            IsDeleted = FakeIsDeleted.CreateValid(fixture),
            DeletedAt = FakeDeletedAt.CreateValid(fixture),
            CreatedAt = FakeCreatedAt.CreateValid(fixture),
            UpdatedAt = FakeUpdatedAt.CreateValid(fixture)
        };
    }
}