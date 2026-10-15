using AutoFixture;
using Core.Enums;
using Core.Models.OutboxMessages;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.OutboxMessages;

public static class FakeCreateOutboxMessageRequest
{
    public static CreateOutboxMessageRequest CreateValid(IFixture fixture)
    {
        return new CreateOutboxMessageRequest
        {
            OrganizationId = FakeOrganizationId.CreateValid(fixture),
            Payload = FakePayload.CreateValid(fixture),
            Channel = NotificationChannel.Webhook,
            Destination = FakeDestination.CreateValid(fixture),
            SecretKey = FakeSecretKey.CreateValid(fixture)
        };
    }
}