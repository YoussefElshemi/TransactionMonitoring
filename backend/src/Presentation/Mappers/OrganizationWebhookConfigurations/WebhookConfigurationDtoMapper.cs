using Core.Models.WebhookConfigurations;
using Presentation.Models.WebhookConfigurations;

namespace Presentation.Mappers.OrganizationWebhookConfigurations;

public static class WebhookConfigurationDtoMapper
{
    public static WebhookConfigurationResponseDto ToDto(this WebhookConfiguration webhookConfiguration)
    {
        return new WebhookConfigurationResponseDto
        {
            Mode = webhookConfiguration.Mode.ToString(),
            WebhookConfigurationId = webhookConfiguration.WebhookConfigurationId,
            OrganizationId = webhookConfiguration.OrganizationId,
            EndpointUrl = webhookConfiguration.EndpointUrl.ToString(),
            IsEnabled = webhookConfiguration.IsEnabled,
            Events = webhookConfiguration.SubscribedEvents.ConvertAll(x => x.ToString()).ToArray(),
            PayloadVersion = webhookConfiguration.PayloadVersion,
            DisabledAt = webhookConfiguration.DisabledAt.HasValue ? webhookConfiguration.DisabledAt.Value : null,
            DisabledReason = webhookConfiguration.DisabledReason?.ToString(),
            CreatedAt = webhookConfiguration.CreatedAt,
            UpdatedAt = webhookConfiguration.UpdatedAt
        };
    }

    public static WebhookConfigurationSecretResponseDto ToSecretDto(this WebhookConfiguration webhookConfiguration)
    {
        return new WebhookConfigurationSecretResponseDto
        {
            Mode = webhookConfiguration.Mode.ToString(),
            WebhookConfigurationId = webhookConfiguration.WebhookConfigurationId,
            OrganizationId = webhookConfiguration.OrganizationId,
            EndpointUrl = webhookConfiguration.EndpointUrl.ToString(),
            SecretKey = webhookConfiguration.SecretKey,
            IsEnabled = webhookConfiguration.IsEnabled,
            Events = webhookConfiguration.SubscribedEvents.ConvertAll(x => x.ToString()).ToArray(),
            PayloadVersion = webhookConfiguration.PayloadVersion,
            CreatedAt = webhookConfiguration.CreatedAt,
            UpdatedAt = webhookConfiguration.UpdatedAt
        };
    }

    public static WebhookEndpointHealthResponseDto ToDto(this WebhookEndpointHealth webhookEndpointHealth)
    {
        return new WebhookEndpointHealthResponseDto
        {
            WebhookConfigurationId = webhookEndpointHealth.WebhookConfigurationId,
            OrganizationId = webhookEndpointHealth.OrganizationId,
            IsEnabled = webhookEndpointHealth.IsEnabled,
            ConsecutiveFailures = webhookEndpointHealth.ConsecutiveFailures,
            LastSucceededAt = webhookEndpointHealth.LastSucceededAt.HasValue ? webhookEndpointHealth.LastSucceededAt.Value : null,
            LastFailedAt = webhookEndpointHealth.LastFailedAt.HasValue ? webhookEndpointHealth.LastFailedAt.Value : null,
            DisabledAt = webhookEndpointHealth.DisabledAt.HasValue ? webhookEndpointHealth.DisabledAt.Value : null,
            DisabledReason = webhookEndpointHealth.DisabledReason?.ToString(),
            From = webhookEndpointHealth.From,
            To = webhookEndpointHealth.To,
            Delivered = webhookEndpointHealth.Delivered,
            Failed = webhookEndpointHealth.Failed,
            Pending = webhookEndpointHealth.Pending
        };
    }
}
