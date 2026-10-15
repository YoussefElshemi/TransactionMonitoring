using System.Net;

namespace Core.Exceptions;

public class NotAvailableInTestModeException(string action)
    : DomainException($"'{action}' is not available in test mode.")
{
    public string Action { get; } = action;

    public override HttpStatusCode GetStatusCode()
    {
        return HttpStatusCode.Conflict;
    }

    public override string GetTitle()
    {
        return "Not available in test mode";
    }

    public override object GetExtensions()
    {
        return new
        {
            Action,
            Mode = "Test"
        };
    }
}
