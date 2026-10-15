using System.Net;
using Core.ValueObjects;

namespace Core.Exceptions;

public class PlanInUseException(PlanName name)
    : DomainException($"The plan '{name}' has subscriptions against it and cannot be deleted. Disable it instead.")
{
    public PlanName Name { get; } = name;

    public override HttpStatusCode GetStatusCode()
    {
        return HttpStatusCode.Conflict;
    }

    public override string GetTitle()
    {
        return "Plan is in use";
    }
}
