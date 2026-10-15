using System.ComponentModel.DataAnnotations.Schema;
using Core.Enums;
using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record RoleAssignmentEntity : BaseEntity, IAuditable
{
    public required Guid RoleAssignmentId { get; init; }
    public required Guid UserId { get; init; }
    public ApplicationUserEntity User { get; init; } = null!;
    public required Guid RoleId { get; init; }
    public ApplicationRoleEntity Role { get; init; } = null!;
    public required int ScopeTypeId { get; init; }
    public PermissionScopeTypeEntity ScopeTypeEntity { get; init; } = null!;
    public required Guid? SubOrganizationId { get; init; }
    public SubOrganizationEntity? SubOrganization { get; init; }
    public required Guid? RuleSetId { get; init; }
    public RuleSetEntity? RuleSet { get; init; }
    public required Guid? QueueId { get; init; }
    public QueueEntity? Queue { get; init; }
    public required Guid AssignedByUserId { get; init; }
    public ApplicationUserEntity AssignedByUser { get; init; } = null!;

    [NotMapped]
    public Guid AuditEntityId => RoleAssignmentId;

    [NotMapped]
    public Guid? AuditOrganizationId => Role?.OrganizationId ?? User?.OrganizationId;

    [NotMapped]
    public AuditableEntityType AuditEntityType => AuditableEntityType.RoleAssignment;

    [NotMapped]
    public IReadOnlyCollection<AuditedProperty> AuditedProperties => AuditedPropertyRegistry.RoleAssignment;
}
