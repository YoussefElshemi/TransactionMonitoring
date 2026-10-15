using Core.Models.OutboxMessages;

namespace Core.Interfaces.Services;

public interface IWebhookService
{
    Task<NotificationDeliveryResult> SendAsync(OutboxMessage outboxMessage, CancellationToken stoppingToken);
}
