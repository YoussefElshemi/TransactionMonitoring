using AutoFixture;
using Presentation.Models.WebhookConfigurations;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.OrganizationWebhookConfigurations;

public static class FakeRecoverOrganizationWebhookConfigurationRequestDto
{
    public static RecoverOrganizationWebhookConfigurationRequestDto CreateValid(IFixture fixture)
    {
        return new RecoverOrganizationWebhookConfigurationRequestDto
        {
            OrganizationId = fixture.Create<Guid>(),
            WebhookConfigurationId = fixture.Create<Guid>()
        };
    }
}