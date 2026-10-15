using Core.Models.WebhookConfigurations;
using Core.ValueObjects;
using Presentation.Models.WebhookConfigurations;

namespace Presentation.Mappers.OrganizationWebhookConfigurations;

public static class UpdateOrganizationWebhookConfigurationRequestMapper
{
    public static UpdateWebhookConfigurationRequest ToModel(this UpdateOrganizationWebhookConfigurationRequestDto updateOrganizationWebhookConfigurationRequestDto)
    {
        return new UpdateWebhookConfigurationRequest
        {
            WebhookConfigurationId = new WebhookConfigurationId(updateOrganizationWebhookConfigurationRequestDto.WebhookConfigurationId),
            EndpointUrl = !string.IsNullOrWhiteSpace(updateOrganizationWebhookConfigurationRequestDto.Body.EndpointUrl) ? new Uri(updateOrganizationWebhookConfigurationRequestDto.Body.EndpointUrl) : null,
            IsEnabled = updateOrganizationWebhookConfigurationRequestDto.Body.IsEnabled.HasValue ? new IsEnabled(updateOrganizationWebhookConfigurationRequestDto.Body.IsEnabled.Value) : null,

            SubscribedEvents = updateOrganizationWebhookConfigurationRequestDto.Body.Events?.Select(x => new WebhookEvent(x)).ToList(),
            PayloadVersion = updateOrganizationWebhookConfigurationRequestDto.Body.PayloadVersion.HasValue
                ? new WebhookPayloadVersion(updateOrganizationWebhookConfigurationRequestDto.Body.PayloadVersion.Value)
                : null
        };
    }
}