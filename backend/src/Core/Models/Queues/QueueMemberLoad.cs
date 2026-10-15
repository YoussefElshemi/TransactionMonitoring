using Core.Models.Users;
using Core.ValueObjects;

namespace Core.Models.Queues;

public record QueueMemberLoad
{
    public required ApplicationUserId UserId { get; init; }
    public required ApplicationUser? User { get; init; }

    public required OpenCaseCount OpenCaseCount { get; init; }
    public required LastAssignedAt? LastAssignedAt { get; init; }
}
