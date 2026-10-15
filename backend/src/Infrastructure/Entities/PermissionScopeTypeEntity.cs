using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record PermissionScopeTypeEntity : BaseEntity
{
    public required int PermissionScopeTypeId { get; init; }
    public required string Name { get; init; }
    public ICollection<RoleAssignmentEntity> RoleAssignments { get; init; } = [];
}
