using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record OutboxMessageEntity : ProcessableEntity, IModeScoped
{
    public required int ModeId { get; init; }

    public required Guid OutboxMessageId { get; init; }
    public required Guid? OrganizationId { get; init; }
    public OrganizationEntity? Organization { get; init; }
    public required string Payload { get; init; }
    public required int ChannelId { get; init; }
    public NotificationChannelEntity Channel { get; init; } = null!;
    public required string Destination { get; init; }
    public required string? SecretKey { get; init; }
    public required Guid? WebhookConfigurationId { get; init; }
    public WebhookConfigurationEntity? WebhookConfiguration { get; init; }
    public required string? Event { get; init; }
    public required int? PayloadVersion { get; init; }
    public required Guid? ReplayOfOutboxMessageId { get; init; }
    public required int Attempts { get; set; }
    public DateTimeOffset? LastAttemptedAt { get; set; }
    public int? ResponseStatusCode { get; set; }
    public string? ResponseBody { get; set; }
    public int? Latency { get; set; }
    public DateTimeOffset? SuppressedAt { get; set; }
}
