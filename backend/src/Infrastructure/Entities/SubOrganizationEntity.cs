using System.ComponentModel.DataAnnotations.Schema;
using Core.Enums;
using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record SubOrganizationEntity : DeletableEntity, IAuditable
{
    public required Guid SubOrganizationId { get; init; }
    public required Guid OrganizationId { get; init; }
    public OrganizationEntity Organization { get; init; } = null!;
    public required string Name { get; set; }
    public required string? Description { get; set; }
    public required string? ExternalReference { get; set; }
    public required bool IsEnabled { get; set; }
    public ICollection<ApplicationUserEntity> ApplicationUsers { get; init; } = [];
    public ICollection<QueueEntity> Queues { get; init; } = [];
    public ICollection<RuleSetEntity> RuleSets { get; init; } = [];
    public ICollection<RoleAssignmentEntity> RoleAssignments { get; init; } = [];

    [NotMapped]
    public Guid AuditEntityId => SubOrganizationId;

    [NotMapped]
    public Guid? AuditOrganizationId => OrganizationId;

    [NotMapped]
    public AuditableEntityType AuditEntityType => AuditableEntityType.SubOrganization;

    [NotMapped]
    public IReadOnlyCollection<AuditedProperty> AuditedProperties => AuditedPropertyRegistry.SubOrganization;
}
