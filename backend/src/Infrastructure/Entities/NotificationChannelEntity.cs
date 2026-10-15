using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record NotificationChannelEntity : BaseEntity
{
    public required int NotificationChannelId { get; init; }
    public required string Name { get; init; }
    public ICollection<NotificationSubscriptionEntity> NotificationSubscriptions { get; init; } = [];
    public ICollection<OutboxMessageEntity> OutboxMessages { get; init; } = [];
}
