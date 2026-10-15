using System.Net;
using Core.ValueObjects;

namespace Core.Exceptions;

public class BuiltInRoleException(RoleName name)
    : DomainException($"The built-in role '{name}' cannot be changed or deleted. Compose a custom role instead.")
{
    public RoleName Name { get; } = name;

    public override HttpStatusCode GetStatusCode()
    {
        return HttpStatusCode.Conflict;
    }

    public override string GetTitle()
    {
        return "Role is built in";
    }
}
