using Core.Models.Notifications;
using Core.ValueObjects;

namespace Core.Interfaces.Services;

public interface IEmailSender
{
    Task<Result> SendAsync(Destination destination, NotificationMessage notificationMessage, CancellationToken stoppingToken);
}
