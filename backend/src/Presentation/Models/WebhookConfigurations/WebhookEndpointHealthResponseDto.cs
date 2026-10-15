namespace Presentation.Models.WebhookConfigurations;

public record WebhookEndpointHealthResponseDto
{
    public required Guid WebhookConfigurationId { get; init; }
    public required Guid OrganizationId { get; init; }
    public required bool IsEnabled { get; init; }
    public required int ConsecutiveFailures { get; init; }
    public required DateTimeOffset? LastSucceededAt { get; init; }
    public required DateTimeOffset? LastFailedAt { get; init; }
    public required DateTimeOffset? DisabledAt { get; init; }
    public required string? DisabledReason { get; init; }

    public required DateTimeOffset From { get; init; }
    public required DateTimeOffset To { get; init; }
    public required int Delivered { get; init; }
    public required int Failed { get; init; }

    public required int Pending { get; init; }
}
