using Core.ValueObjects;

namespace Core.Models.WebhookConfigurations;

public record WebhookEndpointHealth
{
    public required WebhookConfigurationId WebhookConfigurationId { get; init; }
    public required OrganizationId OrganizationId { get; init; }
    public required IsEnabled IsEnabled { get; init; }
    public required ConsecutiveFailures ConsecutiveFailures { get; init; }
    public required LastSucceededAt? LastSucceededAt { get; init; }
    public required LastFailedAt? LastFailedAt { get; init; }
    public required DisabledAt? DisabledAt { get; init; }
    public required DisabledReason? DisabledReason { get; init; }

    public required PeriodStart From { get; init; }
    public required PeriodEnd To { get; init; }
    public required Count Delivered { get; init; }
    public required Count Failed { get; init; }
    public required Count Pending { get; init; }
}
