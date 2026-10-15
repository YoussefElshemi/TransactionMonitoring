using System.Net;

namespace Core.Exceptions;

public class SandboxBusyException()
    : DomainException("A payment simulation is still running. Wait for it to finish before resetting the sandbox.")
{
    public override HttpStatusCode GetStatusCode()
    {
        return HttpStatusCode.Conflict;
    }

    public override string GetTitle()
    {
        return "Sandbox is busy";
    }
}
