using Core.Enums;
using Core.Extensions;
using Core.Models.OutboxMessages;
using Core.Models.WebhookConfigurations;
using Core.Models.Webhooks;
using Core.ValueObjects;

namespace Core.Mappers.Webhooks;

public static class WebhookOutboxMapper
{
    public static List<CreateOutboxMessageRequest> ToOutboxMessageRequests<T>(
        this List<WebhookConfiguration> webhookConfigurations,
        string @event,
        T payload,
        DateTimeOffset timestamp)
    {
        var webhookEvent = new WebhookEvent(@event);

        var subscribed = webhookConfigurations.Where(x => x.IsSubscribedTo(webhookEvent)).ToList();

        if (subscribed.Count == 0)
        {
            return [];
        }

        var payloadsByVersion = subscribed
            .Select(x => x.PayloadVersion)
            .Distinct()
            .ToDictionary(x => x, x => Render(webhookEvent, x, payload, timestamp));

        return subscribed.ConvertAll(x => new CreateOutboxMessageRequest
        {
            OrganizationId = x.OrganizationId,
            Payload = payloadsByVersion[x.PayloadVersion],
            Channel = NotificationChannel.Webhook,
            Destination = new Destination(x.EndpointUrl.ToString()),
            SecretKey = x.SecretKey,
            WebhookConfigurationId = x.WebhookConfigurationId,
            Event = webhookEvent,
            PayloadVersion = x.PayloadVersion
        });
    }

    public static bool IsSubscribedTo(this WebhookConfiguration webhookConfiguration, WebhookEvent webhookEvent)
    {
        return webhookConfiguration.SubscribedEvents.Count == 0 ||
               webhookConfiguration.SubscribedEvents.Contains(webhookEvent);
    }

    public static Payload Render<T>(WebhookEvent webhookEvent, WebhookPayloadVersion version, T payload, DateTimeOffset timestamp)
    {
        var webhookMessage = new WebhookMessage<T>
        {
            Event = webhookEvent,
            Version = version,
            Timestamp = timestamp,
            Payload = payload
        };

        return new Payload(webhookMessage.Serialize().ToBase64String());
    }
}
