using AutoFixture;
using Core.Constants;
using Core.Enums;
using Infrastructure.Entities;

namespace UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

public static class FakeWebhookConfigurationEntity
{
    public static WebhookConfigurationEntity CreateValid(IFixture fixture)
    {
        return new WebhookConfigurationEntity
        {
            ModeId = (int)Mode.Live,
            CreatedAt = fixture.Create<DateTimeOffset>(),
            UpdatedAt = fixture.Create<DateTimeOffset>(),
            IsDeleted = fixture.Create<bool>(),
            DeletedAt = fixture.Create<DateTimeOffset>(),
            WebhookConfigurationId = fixture.Create<Guid>(),
            EndpointUrl = fixture.Create<Uri>().ToString(),
            SecretKey = fixture.Create<string>(),
            IsEnabled = fixture.Create<bool>(),
            OrganizationId = fixture.Create<Guid>(),
            PayloadVersion = WebhookPayloadVersions.Current,
            ConsecutiveFailures = 0,
            Organization = FakeOrganizationEntity.CreateValid(fixture)
        };
    }
}