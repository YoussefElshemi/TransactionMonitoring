using Core.Models.OutboxMessages;
using Core.Models.WebhookConfigurations;
using Core.ValueObjects;

namespace Core.Interfaces.Services;

public interface IWebhookHealthService
{
    Task RecordOutcomeAsync(OutboxMessage outboxMessage, Result result, Attempts attempts, CancellationToken stoppingToken);
    Task<WebhookEndpointHealth> GetAsync(WebhookConfigurationId webhookConfigurationId, CancellationToken stoppingToken);
}
