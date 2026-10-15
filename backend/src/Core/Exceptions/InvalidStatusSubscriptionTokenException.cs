using System.Net;

namespace Core.Exceptions;

public class InvalidStatusSubscriptionTokenException()
    : DomainException("That confirmation link is not valid. Request a new one.")
{
    public override HttpStatusCode GetStatusCode()
    {
        return HttpStatusCode.BadRequest;
    }

    public override string GetTitle()
    {
        return "Invalid subscription link";
    }
}
