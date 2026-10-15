using System.Diagnostics;
using Core.Enums;
using Core.Extensions;
using Core.Interfaces.Services;
using Core.Models.Notifications;
using Core.Models.OutboxMessages;
using Core.ValueObjects;

namespace Infrastructure.Services;

public class EmailNotificationSender(IEmailSender emailSender) : INotificationSender
{
    public NotificationChannel Channel => NotificationChannel.Email;

    public async Task<NotificationDeliveryResult> SendAsync(OutboxMessage outboxMessage, CancellationToken stoppingToken)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var notificationMessage = outboxMessage.Payload.ToString()
                .FromBase64String()
                .TryDeserialize<NotificationMessage>();

            var result = await emailSender.SendAsync(outboxMessage.Destination, notificationMessage, stoppingToken);

            return new NotificationDeliveryResult
            {
                Result = result,
                StatusCode = null,
                ResponseBody = null,
                Latency = new LatencyMilliseconds((int)stopwatch.ElapsedMilliseconds)
            };
        }
        catch (Exception exception)
        {
            return NotificationDeliveryResult.Failed(
                new LatencyMilliseconds((int)stopwatch.ElapsedMilliseconds), exception.Message);
        }
    }
}
