using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record PermissionEntity : BaseEntity
{
    public required int PermissionId { get; init; }
    public required string Name { get; init; }
    public ICollection<RolePermissionEntity> RolePermissions { get; init; } = [];
}
