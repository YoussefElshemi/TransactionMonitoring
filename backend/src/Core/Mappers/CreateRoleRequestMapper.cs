using Core.Models.Roles;
using Core.ValueObjects;

namespace Core.Mappers;

public static class CreateRoleRequestMapper
{
    public static Role ToModel(this CreateRoleRequest createRoleRequest, TimeProvider timeProvider)
    {
        return new Role
        {
            RoleId = new RoleId(Guid.NewGuid()),
            OrganizationId = createRoleRequest.OrganizationId,
            Name = createRoleRequest.Name,
            Description = createRoleRequest.Description,
            IsBuiltIn = new IsBuiltIn(false),
            Permissions = createRoleRequest.Permissions,
            CreatedAt = new CreatedAt(timeProvider.GetUtcNow()),
            UpdatedAt = new UpdatedAt(timeProvider.GetUtcNow()),
            IsDeleted = new IsDeleted(false),
            DeletedAt = null
        };
    }
}
