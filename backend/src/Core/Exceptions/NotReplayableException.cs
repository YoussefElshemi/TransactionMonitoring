using System.Net;

namespace Core.Exceptions;

public class NotReplayableException(string channel)
    : DomainException($"A '{channel}' notification cannot be replayed.")
{
    public string Channel { get; } = channel;

    public override HttpStatusCode GetStatusCode()
    {
        return HttpStatusCode.Conflict;
    }

    public override string GetTitle()
    {
        return "Not replayable";
    }

    public override object GetExtensions()
    {
        return new
        {
            Channel
        };
    }
}
