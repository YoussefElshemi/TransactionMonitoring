using System.ComponentModel.DataAnnotations.Schema;
using Core.Enums;
using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record SubscriptionEntity : DeletableEntity, IAuditable
{
    public required Guid SubscriptionId { get; init; }
    public required Guid OrganizationId { get; init; }
    public OrganizationEntity Organization { get; init; } = null!;
    public required Guid PlanVersionId { get; init; }
    public PlanVersionEntity PlanVersion { get; init; } = null!;
    public required DateTimeOffset EffectiveFrom { get; set; }
    public required DateTimeOffset? EffectiveTo { get; set; }
    public required Guid? CreatedByUserId { get; init; }
    public ApplicationUserEntity? CreatedByUser { get; init; }

    [NotMapped]
    public Guid AuditEntityId => SubscriptionId;

    [NotMapped]
    public Guid? AuditOrganizationId => OrganizationId;

    [NotMapped]
    public AuditableEntityType AuditEntityType => AuditableEntityType.Subscription;

    [NotMapped]
    public IReadOnlyCollection<AuditedProperty> AuditedProperties => AuditedPropertyRegistry.Subscription;
}
