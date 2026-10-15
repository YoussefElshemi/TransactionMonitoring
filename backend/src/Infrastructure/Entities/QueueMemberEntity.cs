using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record QueueMemberEntity : BaseEntity
{
    public required Guid QueueId { get; init; }
    public QueueEntity Queue { get; init; } = null!;
    public required Guid UserId { get; init; }
    public ApplicationUserEntity User { get; init; } = null!;
    public required DateTimeOffset? LastAssignedAt { get; set; }
}
