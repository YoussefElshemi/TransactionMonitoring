using System.Net;
using Core.Enums;

namespace Core.Exceptions;

public class InvitationNotPendingException(InvitationStatus invitationStatus)
    : DomainException($"The invitation is {invitationStatus.ToString().ToLowerInvariant()} and can no longer be changed.")
{
    public InvitationStatus InvitationStatus { get; } = invitationStatus;

    public override HttpStatusCode GetStatusCode()
    {
        return HttpStatusCode.Conflict;
    }

    public override string GetTitle()
    {
        return "Invitation is not pending";
    }

    // Not "Status": it would collide with the RFC 7807 status member, and the handler drops colliding extensions.
    public override object GetExtensions()
    {
        return new
        {
            InvitationStatus = InvitationStatus.ToString()
        };
    }
}
