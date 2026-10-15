using Core.Constants;
using Core.Enums;
using Core.Helpers;
using Core.Models.WebhookConfigurations;
using Core.ValueObjects;

namespace Core.Mappers;

public static class CreateWebhookConfigurationRequestMapper
{
    public static WebhookConfiguration ToModel(this CreateWebhookConfigurationRequest createWebhookConfigurationRequest, TimeProvider timeProvider, Mode mode)
    {
        return new WebhookConfiguration
        {
            Mode = mode,
            WebhookConfigurationId = new WebhookConfigurationId(Guid.NewGuid()),
            OrganizationId = createWebhookConfigurationRequest.OrganizationId,
            EndpointUrl = createWebhookConfigurationRequest.EndpointUrl,
            SecretKey = SecretKeyHelper.Generate(),
            IsEnabled = createWebhookConfigurationRequest.IsEnabled,

            SubscribedEvents = createWebhookConfigurationRequest.SubscribedEvents.Distinct().ToList(),
            PayloadVersion = createWebhookConfigurationRequest.PayloadVersion
                             ?? new WebhookPayloadVersion(WebhookPayloadVersions.Current),
            ConsecutiveFailures = new ConsecutiveFailures(0),
            LastSucceededAt = null,
            LastFailedAt = null,
            DisabledAt = null,
            DisabledReason = null,
            CreatedAt = new CreatedAt(timeProvider.GetUtcNow()),
            UpdatedAt = new UpdatedAt(timeProvider.GetUtcNow()),
            IsDeleted = new IsDeleted(false),
            DeletedAt = null
        };
    }
}
