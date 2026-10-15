using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record QueueRuleEntity : BaseEntity
{
    public required Guid QueueId { get; init; }
    public QueueEntity Queue { get; init; } = null!;
    public required Guid RuleId { get; init; }
    public RuleEntity Rule { get; init; } = null!;
}
