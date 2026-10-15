using Core.Enums;
using Core.Models.Roles;
using Core.ValueObjects;
using Infrastructure.Entities;

namespace Infrastructure.Mappers;

public static class RoleMapper
{
    public static Role ToModel(this ApplicationRoleEntity roleEntity)
    {
        return new Role
        {
            RoleId = new RoleId(roleEntity.Id),
            OrganizationId = roleEntity.OrganizationId.HasValue ? new OrganizationId(roleEntity.OrganizationId.Value) : null,
            Name = new RoleName(roleEntity.Name!),
            Description = !string.IsNullOrWhiteSpace(roleEntity.Description) ? new RoleDescription(roleEntity.Description) : null,
            IsBuiltIn = new IsBuiltIn(roleEntity.IsBuiltIn),
            Permissions = roleEntity.RolePermissions.Select(x => (Permission)x.PermissionId).ToList(),
            CreatedAt = new CreatedAt(roleEntity.CreatedAt),
            UpdatedAt = new UpdatedAt(roleEntity.UpdatedAt),
            IsDeleted = new IsDeleted(roleEntity.IsDeleted),
            DeletedAt = roleEntity.DeletedAt.HasValue ? new DeletedAt(roleEntity.DeletedAt.Value) : null
        };
    }

    public static ApplicationRoleEntity ToEntity(this Role role)
    {
        return new ApplicationRoleEntity
        {
            Id = role.RoleId,
            OrganizationId = role.OrganizationId.HasValue ? (Guid)role.OrganizationId.Value : null,

            // Identity looks roles up by normalized name only.
            Name = role.Name,
            NormalizedName = role.Name.ToString().ToUpperInvariant(),
            Description = role.Description?.ToString(),
            IsBuiltIn = role.IsBuiltIn,
            CreatedAt = role.CreatedAt,
            UpdatedAt = role.UpdatedAt,
            IsDeleted = role.IsDeleted,
            DeletedAt = role.DeletedAt
        };
    }
}
