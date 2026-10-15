using AutoFixture;
using Core.Constants;
using Core.Enums;
using Core.Models.WebhookConfigurations;
using Core.ValueObjects;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.WebhookConfigurations;

public static class FakeWebhookConfiguration
{
    public static WebhookConfiguration CreateValid(IFixture fixture)
    {
        return new WebhookConfiguration
        {
            Mode = Mode.Live,
            CreatedAt = FakeCreatedAt.CreateValid(fixture),
            UpdatedAt = FakeUpdatedAt.CreateValid(fixture),
            IsDeleted = FakeIsDeleted.CreateValid(fixture),
            DeletedAt = FakeDeletedAt.CreateValid(fixture),
            WebhookConfigurationId = FakeWebhookConfigurationId.CreateValid(fixture),
            EndpointUrl = fixture.Create<Uri>(),
            SecretKey = FakeSecretKey.CreateValid(fixture),
            IsEnabled = FakeIsEnabled.CreateValid(fixture),
            OrganizationId = FakeOrganizationId.CreateValid(fixture),

            SubscribedEvents = [],
            PayloadVersion = new WebhookPayloadVersion(WebhookPayloadVersions.Current),
            ConsecutiveFailures = new ConsecutiveFailures(0),
            LastSucceededAt = null,
            LastFailedAt = null,
            DisabledAt = null,
            DisabledReason = null
        };
    }
}