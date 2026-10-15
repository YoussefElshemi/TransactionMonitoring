using Core.ValueObjects;

namespace Core.Models.WebhookConfigurations;

public record UpdateWebhookConfigurationRequest
{
    public required WebhookConfigurationId WebhookConfigurationId { get; init; }
    public required Uri? EndpointUrl { get; init; }
    public required IsEnabled? IsEnabled { get; init; }

    public required List<WebhookEvent>? SubscribedEvents { get; init; }

    public required WebhookPayloadVersion? PayloadVersion { get; init; }
}
