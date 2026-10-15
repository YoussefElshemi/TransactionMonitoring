using AutoFixture;
using Presentation.Models.WebhookConfigurations;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.OrganizationWebhookConfigurations;

public static class FakeOrganizationWebhookConfigurationRequestDto
{
    public static OrganizationWebhookConfigurationRequestDto CreateValid(IFixture fixture)
    {
        return new OrganizationWebhookConfigurationRequestDto
        {
            OrganizationId = fixture.Create<Guid>(),
            WebhookConfigurationId = fixture.Create<Guid>()
        };
    }
}