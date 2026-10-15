using Core.Models.WebhookConfigurations;
using Core.ValueObjects;

namespace Core.Interfaces.Repositories;

public interface IWebhookConfigurationRepository : IPagedRepository<ListOrganizationWebhookConfigurationRequest, WebhookConfiguration>
{
    Task AddAsync(WebhookConfiguration webhookConfiguration, CancellationToken stoppingToken);
    Task<WebhookConfiguration> GetAsync(WebhookConfigurationId webhookConfigurationId, CancellationToken stoppingToken);
    Task<List<WebhookConfiguration>> GetAllAsync(OrganizationId organizationId, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(OrganizationId organizationId, WebhookConfigurationId webhookConfigurationId, IncludeDeleted includeDeleted, CancellationToken stoppingToken);
    Task UpdateAsync(UpdateWebhookConfigurationRequest updateWebhookConfigurationRequest, CancellationToken stoppingToken);
    Task RotateSecretAsync(WebhookConfigurationId webhookConfigurationId, SecretKey secretKey, CancellationToken stoppingToken);
    Task DeleteAsync(WebhookConfigurationId webhookConfigurationId, CancellationToken stoppingToken);
    Task RecoverAsync(WebhookConfigurationId webhookConfigurationId, CancellationToken stoppingToken);

    Task RecordSuccessAsync(WebhookConfigurationId webhookConfigurationId, CancellationToken stoppingToken);

    // One call because one write: a second no-tracking read would attach a duplicate of an already-modified row and the change tracker refuses it.
    Task<WebhookFailureOutcome> RecordFailureAsync(
        WebhookConfigurationId webhookConfigurationId,
        ConsecutiveFailures autoDisableThreshold,
        DisabledReason disabledReason,
        CancellationToken stoppingToken);
}
