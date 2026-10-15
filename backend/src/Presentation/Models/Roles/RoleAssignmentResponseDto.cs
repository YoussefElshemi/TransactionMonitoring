using Presentation.Models.Permissions;

namespace Presentation.Models.Roles;

public record RoleAssignmentResponseDto
{
    public required Guid RoleAssignmentId { get; init; }
    public required Guid UserId { get; init; }
    public required Guid RoleId { get; init; }
    public required string? RoleName { get; init; }
    public required PermissionScopeDto Scope { get; init; }
    public required Guid AssignedByUserId { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}
