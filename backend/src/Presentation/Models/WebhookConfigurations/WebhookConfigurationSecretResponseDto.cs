namespace Presentation.Models.WebhookConfigurations;

public record WebhookConfigurationSecretResponseDto
{
    public required Guid WebhookConfigurationId { get; init; }
    public required Guid OrganizationId { get; init; }
    public required string EndpointUrl { get; init; }
    public required string SecretKey { get; init; }
    public required bool IsEnabled { get; init; }
    public required string[] Events { get; init; }
    public required int PayloadVersion { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }

    public required string Mode { get; init; }
}
