using Core.ValueObjects;

namespace Core.Models.Webhooks;

public record WebhookCatalogue
{
    public required IReadOnlyList<WebhookEventDefinition> Events { get; init; }
    public required WebhookPayloadVersion CurrentPayloadVersion { get; init; }
    public required IReadOnlyList<WebhookPayloadVersion> SupportedPayloadVersions { get; init; }
    public required IReadOnlyList<SourceIpRange> SourceIpRanges { get; init; }
}
