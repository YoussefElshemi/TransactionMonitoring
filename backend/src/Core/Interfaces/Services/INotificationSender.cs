using Core.Enums;
using Core.Models.OutboxMessages;

namespace Core.Interfaces.Services;

public interface INotificationSender
{
    NotificationChannel Channel { get; }

    Task<NotificationDeliveryResult> SendAsync(OutboxMessage outboxMessage, CancellationToken stoppingToken);
}
