using Core.Models.OutboxMessages;
using Core.Models.Sandbox;
using Core.ValueObjects;

namespace Core.Interfaces.Services;

public interface ISandboxService : IPagedService<ListWebhookDeliveryRequest, OutboxMessage>
{
    Task<SandboxReset> ResetAsync(OrganizationId organizationId, CancellationToken stoppingToken);

    Task<OutboxMessage> ReplayDeliveryAsync(OutboxMessageId outboxMessageId, CancellationToken stoppingToken);

    Task<Exists> DeliveryExistsAsync(OrganizationId organizationId, OutboxMessageId outboxMessageId, CancellationToken stoppingToken);
}
