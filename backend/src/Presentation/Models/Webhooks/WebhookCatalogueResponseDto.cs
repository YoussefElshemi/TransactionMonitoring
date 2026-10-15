namespace Presentation.Models.Webhooks;

public record WebhookEventDefinitionResponseDto
{
    public required string Event { get; init; }
    public required string Category { get; init; }
    public required string Description { get; init; }

    public required bool IsSubscribable { get; init; }
    public required int IntroducedInVersion { get; init; }
}

public record WebhookCatalogueResponseDto
{
    public required WebhookEventDefinitionResponseDto[] Events { get; init; }
    public required int CurrentPayloadVersion { get; init; }
    public required int[] SupportedPayloadVersions { get; init; }

    public required string[] SourceIpRanges { get; init; }
}
