using System.ComponentModel.DataAnnotations.Schema;
using Core.Enums;
using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record ContractDiscountEntity : DeletableEntity, IAuditable
{
    public required Guid ContractDiscountId { get; init; }
    public required Guid OrganizationId { get; init; }
    public OrganizationEntity Organization { get; init; } = null!;
    public required int DiscountTypeId { get; set; }
    public DiscountTypeEntity DiscountType { get; init; } = null!;
    public required decimal Value { get; set; }
    public required string Description { get; set; }
    public required DateTimeOffset EffectiveFrom { get; set; }
    public required DateTimeOffset? EffectiveTo { get; set; }
    public required Guid? CreatedByUserId { get; init; }
    public ApplicationUserEntity? CreatedByUser { get; init; }

    [NotMapped]
    public Guid AuditEntityId => ContractDiscountId;

    [NotMapped]
    public Guid? AuditOrganizationId => OrganizationId;

    [NotMapped]
    public AuditableEntityType AuditEntityType => AuditableEntityType.ContractDiscount;

    [NotMapped]
    public IReadOnlyCollection<AuditedProperty> AuditedProperties => AuditedPropertyRegistry.ContractDiscount;
}
