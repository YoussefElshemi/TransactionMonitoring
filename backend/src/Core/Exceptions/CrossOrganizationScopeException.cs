using System.Net;

namespace Core.Exceptions;

public class CrossOrganizationScopeException()
    : DomainException("The scope names a queue, rule set or sub-organization outside the user's organization.")
{
    public override HttpStatusCode GetStatusCode()
    {
        return HttpStatusCode.BadRequest;
    }

    public override string GetTitle()
    {
        return "Scope is outside the organization";
    }
}
