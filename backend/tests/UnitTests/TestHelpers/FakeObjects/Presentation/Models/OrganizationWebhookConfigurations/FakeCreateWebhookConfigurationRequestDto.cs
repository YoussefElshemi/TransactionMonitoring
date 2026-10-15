using AutoFixture;
using Presentation.Models.WebhookConfigurations;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.OrganizationWebhookConfigurations;

public static class FakeCreateWebhookConfigurationRequestDto
{
    public static CreateWebhookConfigurationRequestDto CreateValid(IFixture fixture)
    {
        return new CreateWebhookConfigurationRequestDto
        {
            OrganizationId = fixture.Create<Guid>(),
            Body = new CreateWebhookConfigurationRequestBodyDto
            {
                EndpointUrl = fixture.Create<Uri>().ToString(),
                IsEnabled = fixture.Create<bool>()
            }
        };
    }

    public static CreateWebhookConfigurationRequestDto CreateInvalid(IFixture fixture)
    {
        return new CreateWebhookConfigurationRequestDto
        {
            OrganizationId = fixture.Create<Guid>(),
            Body = new CreateWebhookConfigurationRequestBodyDto
            {
                EndpointUrl = fixture.Create<string>(),
                IsEnabled = fixture.Create<bool>()
            }
        };
    }
}