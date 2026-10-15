using Core.Models.WebhookConfigurations;
using Core.ValueObjects;

namespace Core.Interfaces.Services;

public interface IWebhookConfigurationService : IPagedService<ListOrganizationWebhookConfigurationRequest, WebhookConfiguration>
{
    Task<WebhookConfiguration> CreateAsync(CreateWebhookConfigurationRequest createWebhookConfigurationRequest, CancellationToken stoppingToken);
    Task UpdateAsync(UpdateWebhookConfigurationRequest updateWebhookConfigurationRequest, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(OrganizationId organizationId, WebhookConfigurationId webhookConfigurationId, IncludeDeleted includeDeleted, CancellationToken stoppingToken);
    Task<WebhookConfiguration> GetAsync(WebhookConfigurationId webhookConfigurationId, CancellationToken stoppingToken);
    Task<List<WebhookConfiguration>> GetAllAsync(OrganizationId organizationId, CancellationToken stoppingToken);
    Task<Result> TestAsync(WebhookConfigurationId webhookConfigurationId, CancellationToken stoppingToken);
    Task<WebhookConfiguration> RotateSecretAsync(WebhookConfigurationId webhookConfigurationId, CancellationToken stoppingToken);
    Task DeleteAsync(WebhookConfigurationId webhookConfigurationId, CancellationToken stoppingToken);
    Task RecoverAsync(WebhookConfigurationId webhookConfigurationId, CancellationToken stoppingToken);
}