using Core.ValueObjects;

namespace Core.Models.Invitations;

public record AcceptInvitationRequest
{
    public required InvitationToken InvitationToken { get; init; }

    public required UserName? UserName { get; init; }
    public required Password? Password { get; init; }
}
