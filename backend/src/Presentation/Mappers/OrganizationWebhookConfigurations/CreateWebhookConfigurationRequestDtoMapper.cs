using Core.Models.WebhookConfigurations;
using Core.ValueObjects;
using Presentation.Models.WebhookConfigurations;

namespace Presentation.Mappers.OrganizationWebhookConfigurations;

public static class CreateWebhookConfigurationRequestDtoMapper
{
    public static CreateWebhookConfigurationRequest ToModel(this CreateWebhookConfigurationRequestDto createWebhookConfigurationRequestDto)
    {
        return new CreateWebhookConfigurationRequest
        {
            OrganizationId = new OrganizationId(createWebhookConfigurationRequestDto.OrganizationId),
            EndpointUrl = new Uri(createWebhookConfigurationRequestDto.Body.EndpointUrl),
            IsEnabled = new IsEnabled(createWebhookConfigurationRequestDto.Body.IsEnabled),

            SubscribedEvents = createWebhookConfigurationRequestDto.Body.Events?.Select(x => new WebhookEvent(x)).ToList() ?? [],
            PayloadVersion = createWebhookConfigurationRequestDto.Body.PayloadVersion.HasValue
                ? new WebhookPayloadVersion(createWebhookConfigurationRequestDto.Body.PayloadVersion.Value)
                : null
        };
    }
}
