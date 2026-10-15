using Core.Enums;
using Core.Models.Invitations;

namespace Core.Helpers;

public static class InvitationStatusResolver
{
    public static InvitationStatus Resolve(Invitation invitation, DateTimeOffset now)
    {
        if (invitation.AcceptedAt.HasValue)
        {
            return InvitationStatus.Accepted;
        }

        if (invitation.RevokedAt.HasValue)
        {
            return InvitationStatus.Revoked;
        }

        return now >= invitation.ExpiresAt ? InvitationStatus.Expired : InvitationStatus.Pending;
    }

    public static bool IsPending(Invitation invitation, DateTimeOffset now)
    {
        return Resolve(invitation, now) == InvitationStatus.Pending;
    }
}
