using Core.Enums;
using Core.Models.BaseModels;
using Core.ValueObjects;

namespace Core.Models.Roles;

public record Role : DeletableModel
{
    public required RoleId RoleId { get; init; }

    public required OrganizationId? OrganizationId { get; init; }

    public required RoleName Name { get; init; }
    public required RoleDescription? Description { get; init; }

    public required IsBuiltIn IsBuiltIn { get; init; }

    public required List<Permission> Permissions { get; init; }
}
