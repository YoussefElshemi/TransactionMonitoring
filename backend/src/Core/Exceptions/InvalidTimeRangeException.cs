using System.Net;

namespace Core.Exceptions;

public class InvalidTimeRangeException(string subject)
    : DomainException($"A {subject} cannot end before it starts.")
{
    public override HttpStatusCode GetStatusCode()
    {
        return HttpStatusCode.BadRequest;
    }

    public override string GetTitle()
    {
        return "Invalid time range";
    }
}
