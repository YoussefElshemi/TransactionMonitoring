using System.Net;
using Core.Models.Roles;

namespace Core.Exceptions;

public class RoleInUseException(RoleUsage usage)
    : DomainException(
        $"The role '{usage.Name}' cannot be deleted while {usage.AssignmentCount} assignment(s) still name it. " +
        "Revoke those assignments first.")
{
    public RoleUsage Usage { get; } = usage;

    public override HttpStatusCode GetStatusCode()
    {
        return HttpStatusCode.Conflict;
    }

    public override string GetTitle()
    {
        return "Role is in use";
    }

    public override object GetExtensions()
    {
        return new
        {
            AssignmentCount = (int)Usage.AssignmentCount,
            UserIds = Usage.UserIds.ConvertAll(x => x.ToString())
        };
    }
}
