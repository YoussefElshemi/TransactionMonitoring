using Core.ValueObjects;

namespace Core.Models.Invitations;

public record AcceptedInvitation
{
    public required Invitation Invitation { get; init; }
    public required ApplicationUserId UserId { get; init; }

    public required IsCreated IsCreated { get; init; }
}
