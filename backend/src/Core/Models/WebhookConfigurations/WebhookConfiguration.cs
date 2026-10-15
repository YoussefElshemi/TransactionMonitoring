using Core.Enums;
using Core.Models.BaseModels;
using Core.ValueObjects;

namespace Core.Models.WebhookConfigurations;

public record WebhookConfiguration : DeletableModel
{
    public required WebhookConfigurationId WebhookConfigurationId { get; init; }
    public required OrganizationId OrganizationId { get; init; }
    public required Uri EndpointUrl { get; init; }
    public required SecretKey SecretKey { get; init; }
    public required IsEnabled IsEnabled { get; init; }

    public required List<WebhookEvent> SubscribedEvents { get; init; }

    public required WebhookPayloadVersion PayloadVersion { get; init; }

    public required ConsecutiveFailures ConsecutiveFailures { get; init; }
    public required LastSucceededAt? LastSucceededAt { get; init; }
    public required LastFailedAt? LastFailedAt { get; init; }

    public required DisabledAt? DisabledAt { get; init; }
    public required DisabledReason? DisabledReason { get; init; }

    public required Mode Mode { get; init; }
}
