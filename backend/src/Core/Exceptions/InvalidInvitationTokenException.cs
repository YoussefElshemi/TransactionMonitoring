using System.Net;

namespace Core.Exceptions;

public class InvalidInvitationTokenException()
    : DomainException("That invitation link is not valid. Ask for a new one.")
{
    public override HttpStatusCode GetStatusCode()
    {
        return HttpStatusCode.BadRequest;
    }

    public override string GetTitle()
    {
        return "Invalid invitation link";
    }
}
