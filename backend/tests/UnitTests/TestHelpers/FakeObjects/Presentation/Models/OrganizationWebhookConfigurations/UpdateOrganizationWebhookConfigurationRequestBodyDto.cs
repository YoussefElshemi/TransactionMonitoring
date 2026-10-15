using AutoFixture;
using Presentation.Models.WebhookConfigurations;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.OrganizationWebhookConfigurations;

public static class FakeUpdateOrganizationWebhookConfigurationRequestDto
{
    public static UpdateOrganizationWebhookConfigurationRequestDto CreateValid(IFixture fixture)
    {
        return new UpdateOrganizationWebhookConfigurationRequestDto
        {
            OrganizationId = fixture.Create<Guid>(),
            WebhookConfigurationId = fixture.Create<Guid>(),
            Body = new UpdateWebhookConfigurationRequestBodyDto
            {
                EndpointUrl = fixture.Create<Uri>().ToString(),
                IsEnabled = fixture.Create<bool>()
            }
        };
    }
}