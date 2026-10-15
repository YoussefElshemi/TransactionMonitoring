using System.ComponentModel.DataAnnotations.Schema;
using Core.Enums;
using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record AvailabilityCommitmentEntity : DeletableEntity, IAuditable
{
    public required Guid AvailabilityCommitmentId { get; init; }
    public required Guid OrganizationId { get; init; }
    public OrganizationEntity Organization { get; init; } = null!;
    public required decimal AvailabilityTarget { get; set; }
    public required int LatencyTargetMilliseconds { get; set; }
    public required int LatencyPercentile { get; set; }
    public required DateTimeOffset EffectiveFrom { get; set; }
    public required DateTimeOffset? EffectiveTo { get; set; }
    public required Guid? CreatedByUserId { get; init; }
    public ApplicationUserEntity? CreatedByUser { get; init; }
    public ICollection<ServiceCreditBandEntity> CreditBands { get; init; } = [];

    [NotMapped]
    public Guid AuditEntityId => AvailabilityCommitmentId;

    [NotMapped]
    public Guid? AuditOrganizationId => OrganizationId;

    [NotMapped]
    public AuditableEntityType AuditEntityType => AuditableEntityType.AvailabilityCommitment;

    [NotMapped]
    public IReadOnlyCollection<AuditedProperty> AuditedProperties => AuditedPropertyRegistry.AvailabilityCommitment;
}
