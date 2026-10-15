using Core.Models.BaseModels;
using Core.Models.Users;
using Core.ValueObjects;

namespace Core.Models.Queues;

public record QueueMember : BaseModel
{
    public required QueueId QueueId { get; init; }
    public required ApplicationUserId UserId { get; init; }
    public required ApplicationUser? User { get; init; }

    public required LastAssignedAt? LastAssignedAt { get; init; }
}
