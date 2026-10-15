using System.Net;

namespace Core.Exceptions;

public class InvitationAlreadyMemberException(bool isSameOrganization)
    : DomainException(
        isSameOrganization
            ? "That address already belongs to this organisation."
            : "That address already belongs to another organisation.")
{
    public bool IsSameOrganization { get; } = isSameOrganization;

    public override HttpStatusCode GetStatusCode()
    {
        return HttpStatusCode.Conflict;
    }

    public override string GetTitle()
    {
        return "Address is already in use";
    }

    public override object GetExtensions()
    {
        return new
        {
            SameOrganization = IsSameOrganization
        };
    }
}
