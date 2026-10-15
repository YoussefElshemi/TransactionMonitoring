using System.ComponentModel.DataAnnotations.Schema;
using Core.Enums;
using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record CommitmentEntity : DeletableEntity, IAuditable
{
    public required Guid CommitmentId { get; init; }
    public required Guid OrganizationId { get; init; }
    public OrganizationEntity Organization { get; init; } = null!;
    public required decimal Amount { get; set; }
    public required DateTimeOffset EffectiveFrom { get; set; }
    public required DateTimeOffset EffectiveTo { get; set; }
    public required Guid? CreatedByUserId { get; init; }
    public ApplicationUserEntity? CreatedByUser { get; init; }

    [NotMapped]
    public Guid AuditEntityId => CommitmentId;

    [NotMapped]
    public Guid? AuditOrganizationId => OrganizationId;

    [NotMapped]
    public AuditableEntityType AuditEntityType => AuditableEntityType.Commitment;

    [NotMapped]
    public IReadOnlyCollection<AuditedProperty> AuditedProperties => AuditedPropertyRegistry.Commitment;
}
