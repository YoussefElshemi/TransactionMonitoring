using System.Net;

namespace Core.Exceptions;

public class NotAvailableInLiveModeException(string action)
    : DomainException($"'{action}' is only available in test mode.")
{
    public string Action { get; } = action;

    public override HttpStatusCode GetStatusCode()
    {
        return HttpStatusCode.Conflict;
    }

    public override string GetTitle()
    {
        return "Only available in test mode";
    }

    public override object GetExtensions()
    {
        return new
        {
            Action,
            Mode = "Live"
        };
    }
}
