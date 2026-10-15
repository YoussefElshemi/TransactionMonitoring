using System.Diagnostics;
using System.Net.Mime;
using System.Text;
using Core.Enums;
using Core.Extensions;
using Core.Interfaces.Services;
using Core.Models.Notifications;
using Core.Models.OutboxMessages;
using Core.ValueObjects;

namespace Infrastructure.Services;

public class ChatNotificationSender(IHttpClientFactory httpClientFactory) : INotificationSender
{
    public NotificationChannel Channel => NotificationChannel.Chat;

    public async Task<NotificationDeliveryResult> SendAsync(OutboxMessage outboxMessage, CancellationToken stoppingToken)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var notificationMessage = outboxMessage.Payload.ToString()
                .FromBase64String()
                .TryDeserialize<NotificationMessage>();

            var chatPayload = new { text = $"*{notificationMessage.Subject}*\n{notificationMessage.Body}" };

            var httpRequestMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(outboxMessage.Destination),
                Content = new StringContent(chatPayload.Serialize(), Encoding.UTF8, MediaTypeNames.Application.Json)
            };

            using var client = httpClientFactory.CreateClient();
            var response = await client.SendAsync(httpRequestMessage, stoppingToken);

            return new NotificationDeliveryResult
            {
                Result = new Result(response.IsSuccessStatusCode),
                StatusCode = new StatusCode((int)response.StatusCode),
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
