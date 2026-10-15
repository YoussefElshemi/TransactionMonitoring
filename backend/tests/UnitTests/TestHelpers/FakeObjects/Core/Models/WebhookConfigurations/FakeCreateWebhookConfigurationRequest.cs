using AutoFixture;
using Core.Models.WebhookConfigurations;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.WebhookConfigurations;

public static class FakeCreateWebhookConfigurationRequest
{
    public static CreateWebhookConfigurationRequest CreateValid(IFixture fixture)
    {
        return new CreateWebhookConfigurationRequest
        {
            EndpointUrl = fixture.Create<Uri>(),
            IsEnabled = FakeIsEnabled.CreateValid(fixture),
            OrganizationId = FakeOrganizationId.CreateValid(fixture),
            SubscribedEvents = [],
            PayloadVersion = null
        };
    }
}