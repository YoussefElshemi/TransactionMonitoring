using AutoFixture;
using Core.Constants;
using Core.Enums;
using Core.Extensions;
using Infrastructure.Entities;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

public static class FakeOutboxMessageEntity
{
    public static OutboxMessageEntity CreateValid(IFixture fixture)
    {
        return new OutboxMessageEntity
        {
            ModeId = (int)Mode.Live,

            // Null: an invented organisation id would violate the foreign key.
            OrganizationId = null,
            CreatedAt = fixture.Create<DateTimeOffset>(),
            UpdatedAt = fixture.Create<DateTimeOffset>(),
            IsDeleted = fixture.Create<bool>(),
            DeletedAt = fixture.Create<DateTimeOffset>(),
            OutboxMessageId = fixture.Create<Guid>(),
            ChannelId = (int)NotificationChannel.Webhook,
            Destination = fixture.Create<Uri>().ToString(),
            SecretKey = fixture.Create<string>(),

            // Null: an invented configuration id would violate the foreign key.
            WebhookConfigurationId = null,
            Event = WebhookEvents.CaseCreated,
            PayloadVersion = WebhookPayloadVersions.Current,
            ReplayOfOutboxMessageId = null,
            Attempts = fixture.Create<int>(),
            IsProcessed = fixture.Create<bool>(),
            ProcessedAt = fixture.Create<DateTimeOffset>(),
            Payload = FakeJsonObject.CreateValid(fixture).Serialize().ToBase64String()
        };
    }
}