using System.Diagnostics;
using System.Net.Mime;
using System.Security.Cryptography;
using System.Text;
using Core.Configs;
using Core.Enums;
using Core.Extensions;
using Core.Interfaces.Services;
using Core.Models.OutboxMessages;
using Core.ValueObjects;
using Infrastructure.Constants;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

public class WebhookService(IHttpClientFactory httpClientFactory, IOptions<AppConfig> appConfig) : IWebhookService, INotificationSender
{
    public NotificationChannel Channel => NotificationChannel.Webhook;

    public async Task<NotificationDeliveryResult> SendAsync(OutboxMessage outboxMessage, CancellationToken stoppingToken)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var payload = outboxMessage.Payload.ToString().FromBase64String();

            var signature = GenerateSignature(outboxMessage.SecretKey!.Value, payload);

            var httpRequestMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(outboxMessage.Destination),
                Content = new StringContent(payload, Encoding.UTF8, MediaTypeNames.Application.Json),
                Headers =
                {
                    { HeaderNames.Signature, signature },
                    { HeaderNames.CorrelationId, outboxMessage.OutboxMessageId.ToString() }
                }
            };

            AddWebhookHeaders(httpRequestMessage, outboxMessage);

            using var client = httpClientFactory.CreateClient();
            var response = await client.SendAsync(httpRequestMessage, stoppingToken);

            var responseBody = await ReadResponseBodyAsync(response, stoppingToken);

            return new NotificationDeliveryResult
            {
                Result = new Result(response.IsSuccessStatusCode),
                StatusCode = new StatusCode((int)response.StatusCode),
                ResponseBody = responseBody,
                Latency = new LatencyMilliseconds((int)stopwatch.ElapsedMilliseconds)
            };
        }
        catch (Exception exception)
        {
            return NotificationDeliveryResult.Failed(
                new LatencyMilliseconds((int)stopwatch.ElapsedMilliseconds), exception.Message);
        }
    }

    private static void AddWebhookHeaders(HttpRequestMessage httpRequestMessage, OutboxMessage outboxMessage)
    {
        if (outboxMessage.Event.HasValue)
        {
            httpRequestMessage.Headers.Add(HeaderNames.WebhookEvent, outboxMessage.Event.Value.ToString());
        }

        if (outboxMessage.PayloadVersion.HasValue)
        {
            httpRequestMessage.Headers.Add(HeaderNames.WebhookVersion, outboxMessage.PayloadVersion.Value.ToString());
        }

        if (outboxMessage.ReplayOfOutboxMessageId.HasValue)
        {
            // Without this a receiver deduplicating on the correlation id processes a replay twice.
            httpRequestMessage.Headers.Add(HeaderNames.WebhookReplayOf, outboxMessage.ReplayOfOutboxMessageId.Value.ToString());
        }
    }

    private async Task<ResponseBody?> ReadResponseBodyAsync(HttpResponseMessage response, CancellationToken stoppingToken)
    {
        var body = await response.Content.ReadAsStringAsync(stoppingToken);

        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        var limit = appConfig.Value.WebhookConfig.ResponseBodyLimit;

        return new ResponseBody(body.Length <= limit ? body : body[..limit]);
    }

    private static Signature GenerateSignature(SecretKey secretKey, string payload)
    {
        var secretKeyBytes = Encoding.UTF8.GetBytes(secretKey.ToString());
        var payloadBytes = Encoding.UTF8.GetBytes(payload);

        using var hmac = new HMACSHA256(secretKeyBytes);
        var hashBytes = hmac.ComputeHash(payloadBytes);
        var signature = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

        return new Signature(signature);
    }
}
