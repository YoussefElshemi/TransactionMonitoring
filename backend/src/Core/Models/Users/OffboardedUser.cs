using Core.ValueObjects;

namespace Core.Models.Users;

public record OffboardedUser
{
    public required ApplicationUserId UserId { get; init; }
    public required ApplicationUserId? SuccessorUserId { get; init; }
    public required Count ReassignedCaseCount { get; init; }
    public required Count QueueMembershipCount { get; init; }
    public required Count RevokedRoleAssignmentCount { get; init; }
    public required Count RevokedInvitationCount { get; init; }
}
