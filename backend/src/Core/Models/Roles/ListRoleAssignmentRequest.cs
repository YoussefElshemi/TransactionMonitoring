using Core.Models.Common;
using Core.ValueObjects;

namespace Core.Models.Roles;

public record ListRoleAssignmentRequest : PagedRequest
{
    public required OrganizationId OrganizationId { get; init; }
    public required ApplicationUserId? UserId { get; init; }
    public required RoleId? RoleId { get; init; }
}
