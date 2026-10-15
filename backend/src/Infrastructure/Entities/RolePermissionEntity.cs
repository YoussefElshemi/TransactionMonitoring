using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record RolePermissionEntity : BaseEntity
{
    public required Guid RoleId { get; init; }
    public ApplicationRoleEntity Role { get; init; } = null!;
    public required int PermissionId { get; init; }
    public PermissionEntity Permission { get; init; } = null!;
}
