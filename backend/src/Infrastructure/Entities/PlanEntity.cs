using System.ComponentModel.DataAnnotations.Schema;
using Core.Enums;
using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record PlanEntity : DeletableEntity, IAuditable
{
    public required Guid PlanId { get; init; }
    public required string Name { get; set; }
    public required string? Description { get; set; }

    public required Guid? OrganizationId { get; init; }
    public OrganizationEntity? Organization { get; init; }

    public required bool IsEnabled { get; set; }
    public required int CurrentVersionNumber { get; set; }
    public ICollection<PlanVersionEntity> PlanVersions { get; init; } = [];

    [NotMapped]
    public Guid AuditEntityId => PlanId;

    [NotMapped]
    public Guid? AuditOrganizationId => OrganizationId;

    [NotMapped]
    public AuditableEntityType AuditEntityType => AuditableEntityType.Plan;

    [NotMapped]
    public IReadOnlyCollection<AuditedProperty> AuditedProperties => AuditedPropertyRegistry.Plan;
}
