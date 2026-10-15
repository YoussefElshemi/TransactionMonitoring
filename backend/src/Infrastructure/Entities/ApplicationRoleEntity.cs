using System.ComponentModel.DataAnnotations.Schema;
using Core.Enums;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Entities;

public class ApplicationRoleEntity : IdentityRole<Guid>, IAuditable
{
    public Guid? OrganizationId { get; set; }
    public OrganizationEntity? Organization { get; init; }

    public string? Description { get; set; }

    public bool IsBuiltIn { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    public uint RowVersion { get; init; }

    public ICollection<RolePermissionEntity> RolePermissions { get; init; } = [];
    public ICollection<RoleAssignmentEntity> RoleAssignments { get; init; } = [];

    [NotMapped]
    public Guid AuditEntityId => Id;

    [NotMapped]
    public Guid? AuditOrganizationId => OrganizationId;

    [NotMapped]
    public AuditableEntityType AuditEntityType => AuditableEntityType.Role;

    [NotMapped]
    public IReadOnlyCollection<AuditedProperty> AuditedProperties => AuditedPropertyRegistry.Role;
}
