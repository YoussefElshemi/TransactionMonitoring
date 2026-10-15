using System.Net;
using Core.Constants;

namespace Core.Exceptions;

public class SandboxTriggerException()
    : DomainException("The payload carried the reserved value that forces a failure in test mode.")
{
    public override HttpStatusCode GetStatusCode()
    {
        return HttpStatusCode.InternalServerError;
    }

    public override string GetTitle()
    {
        return "Forced sandbox failure";
    }

    public override object GetExtensions()
    {
        return new
        {
            Trigger = SandboxTriggerConstants.Prefix + "error",
            Mode = "Test"
        };
    }
}
