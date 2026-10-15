using Core.Models.OutboxMessages;
using Core.ValueObjects;

namespace Core.Interfaces.Services;

public interface IWebhookDeliveryService : IPagedService<ListWebhookDeliveryRequest, OutboxMessage>
{
    Task<OutboxMessage> GetAsync(OutboxMessageId outboxMessageId, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(OrganizationId organizationId, OutboxMessageId outboxMessageId, CancellationToken stoppingToken);
    Task<OutboxMessage> ReplayAsync(OutboxMessageId outboxMessageId, CancellationToken stoppingToken);
    Task<WebhookReplayBatch> ReplayRangeAsync(ReplayWebhookDeliveryRangeRequest replayWebhookDeliveryRangeRequest, CancellationToken stoppingToken);
}
