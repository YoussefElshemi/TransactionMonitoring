using Core.Enums;
using Core.Models.WebhookConfigurations;
using Core.ValueObjects;
using Infrastructure.Entities;

namespace Infrastructure.Mappers;

public static class WebhookConfigurationMapper
{
    public static WebhookConfiguration ToModel(this WebhookConfigurationEntity webhookConfigurationEntity)
    {
        return new WebhookConfiguration
        {
            Mode = (Mode)webhookConfigurationEntity.ModeId,
            WebhookConfigurationId = new WebhookConfigurationId(webhookConfigurationEntity.WebhookConfigurationId),
            OrganizationId = new OrganizationId(webhookConfigurationEntity.OrganizationId),
            EndpointUrl = new Uri(webhookConfigurationEntity.EndpointUrl),
            SecretKey = new SecretKey(webhookConfigurationEntity.SecretKey),
            IsEnabled = new IsEnabled(webhookConfigurationEntity.IsEnabled),

            SubscribedEvents = webhookConfigurationEntity.SubscribedEvents
                .Select(x => x.Event)
                .Order(StringComparer.Ordinal)
                .Select(x => new WebhookEvent(x))
                .ToList(),
            PayloadVersion = new WebhookPayloadVersion(webhookConfigurationEntity.PayloadVersion),
            ConsecutiveFailures = new ConsecutiveFailures(webhookConfigurationEntity.ConsecutiveFailures),
            LastSucceededAt = webhookConfigurationEntity.LastSucceededAt.HasValue ? new LastSucceededAt(webhookConfigurationEntity.LastSucceededAt.Value) : null,
            LastFailedAt = webhookConfigurationEntity.LastFailedAt.HasValue ? new LastFailedAt(webhookConfigurationEntity.LastFailedAt.Value) : null,
            DisabledAt = webhookConfigurationEntity.DisabledAt.HasValue ? new DisabledAt(webhookConfigurationEntity.DisabledAt.Value) : null,
            DisabledReason = webhookConfigurationEntity.DisabledReason is null ? null : new DisabledReason(webhookConfigurationEntity.DisabledReason),
            CreatedAt = new CreatedAt(webhookConfigurationEntity.CreatedAt),
            UpdatedAt = new UpdatedAt(webhookConfigurationEntity.UpdatedAt),
            IsDeleted = new IsDeleted(webhookConfigurationEntity.IsDeleted),
            DeletedAt = webhookConfigurationEntity.DeletedAt.HasValue ? new DeletedAt(webhookConfigurationEntity.DeletedAt.Value) : null
        };
    }

    public static WebhookConfigurationEntity ToEntity(this WebhookConfiguration webhookConfiguration)
    {
        return new WebhookConfigurationEntity
        {
            ModeId = (int)webhookConfiguration.Mode,
            WebhookConfigurationId = webhookConfiguration.WebhookConfigurationId,
            OrganizationId = webhookConfiguration.OrganizationId,
            EndpointUrl = webhookConfiguration.EndpointUrl.ToString(),
            SecretKey = webhookConfiguration.SecretKey,
            IsEnabled = webhookConfiguration.IsEnabled,
            SubscribedEvents = webhookConfiguration.SubscribedEvents.ConvertAll(x => new WebhookConfigurationEventEntity
            {
                WebhookConfigurationId = webhookConfiguration.WebhookConfigurationId,
                Event = x,
                CreatedAt = webhookConfiguration.CreatedAt,
                UpdatedAt = webhookConfiguration.UpdatedAt
            }),
            PayloadVersion = webhookConfiguration.PayloadVersion,
            ConsecutiveFailures = webhookConfiguration.ConsecutiveFailures,
            LastSucceededAt = webhookConfiguration.LastSucceededAt,
            LastFailedAt = webhookConfiguration.LastFailedAt,
            DisabledAt = webhookConfiguration.DisabledAt,
            DisabledReason = webhookConfiguration.DisabledReason?.ToString(),
            CreatedAt = webhookConfiguration.CreatedAt,
            UpdatedAt = webhookConfiguration.UpdatedAt,
            IsDeleted = webhookConfiguration.IsDeleted,
            DeletedAt = webhookConfiguration.DeletedAt
        };
    }
}
