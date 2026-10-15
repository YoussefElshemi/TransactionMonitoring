using System.Net;

namespace Core.Exceptions;

public class SubscriptionTimelineException(string message) : DomainException(message)
{
    public override HttpStatusCode GetStatusCode()
    {
        return HttpStatusCode.Conflict;
    }

    public override string GetTitle()
    {
        return "Subscription conflicts with the existing timeline";
    }
}
