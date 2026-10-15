using Core.ValueObjects;

namespace Core.Models.Invitations;

public record IssuedInvitation
{
    public required Invitation Invitation { get; init; }
    public required InvitationToken InvitationToken { get; init; }
}
