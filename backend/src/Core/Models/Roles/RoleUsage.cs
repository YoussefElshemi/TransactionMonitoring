using Core.ValueObjects;

namespace Core.Models.Roles;

public record RoleUsage
{
    public required RoleId RoleId { get; init; }
    public required RoleName Name { get; init; }
    public required TotalCount AssignmentCount { get; init; }
    public required List<ApplicationUserId> UserIds { get; init; }
}
