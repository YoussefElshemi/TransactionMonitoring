using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record QueueRoutingPolicyEntity : BaseEntity
{
    public required int QueueRoutingPolicyId { get; init; }
    public required string Name { get; init; }
    public ICollection<QueueEntity> Queues { get; init; } = [];
}
