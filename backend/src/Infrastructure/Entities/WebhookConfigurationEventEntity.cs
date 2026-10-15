using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record WebhookConfigurationEventEntity : BaseEntity
{
    public required Guid WebhookConfigurationId { get; init; }
    public WebhookConfigurationEntity WebhookConfiguration { get; init; } = null!;
    public required string Event { get; init; }
}
