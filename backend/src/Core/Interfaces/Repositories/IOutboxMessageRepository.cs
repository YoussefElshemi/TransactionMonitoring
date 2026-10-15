using Core.Models.Common;
using Core.Models.OutboxMessages;
using Core.ValueObjects;

namespace Core.Interfaces.Repositories;

public interface IOutboxMessageRepository
{
    Task AddAsync(OutboxMessage outboxMessage, CancellationToken stoppingToken);
    Task AddRangeAsync(List<OutboxMessage> outboxMessages, CancellationToken stoppingToken);
    Task<OutboxMessage> GetAsync(OutboxMessageId outboxMessageId, CancellationToken stoppingToken);
    Task<List<OutboxMessage>> ListPendingAsync(CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(OutboxMessageId outboxMessageId, IncludeDeleted includeDeleted, CancellationToken stoppingToken);
    Task UpdateAsync(UpdateOutboxMessageRequest updateOutboxMessageRequest, CancellationToken stoppingToken);
    Task ProcessAsync(OutboxMessageId outboxMessageId, UpdateOutboxMessageRequest updateOutboxMessageRequest, CancellationToken stoppingToken);

    Task SuppressAsync(OutboxMessageId outboxMessageId, CancellationToken stoppingToken);

    Task<Exists> ExistsForOrganizationAsync(OrganizationId organizationId, OutboxMessageId outboxMessageId, CancellationToken stoppingToken);

    Task<PagedResponse<OutboxMessage>> ListAsync(ListWebhookDeliveryRequest listWebhookDeliveryRequest, CancellationToken stoppingToken);

    Task<TotalCount> GetCountAsync(ListWebhookDeliveryRequest listWebhookDeliveryRequest, CancellationToken stoppingToken);

    Task<List<OutboxMessage>> ListForReplayAsync(ReplayWebhookDeliveryRangeRequest replayWebhookDeliveryRangeRequest, Count limit, CancellationToken stoppingToken);

    Task<Count> CountForReplayAsync(ReplayWebhookDeliveryRangeRequest replayWebhookDeliveryRangeRequest, CancellationToken stoppingToken);

    Task<WebhookDeliveryTally> TallyAsync(WebhookConfigurationId webhookConfigurationId, PeriodStart from, PeriodEnd to, CancellationToken stoppingToken);
}
