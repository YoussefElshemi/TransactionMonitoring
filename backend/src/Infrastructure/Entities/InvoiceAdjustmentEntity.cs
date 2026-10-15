using System.ComponentModel.DataAnnotations.Schema;
using Core.Enums;
using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record InvoiceAdjustmentEntity : DeletableEntity, IAuditable
{
    public required Guid InvoiceAdjustmentId { get; init; }
    public required Guid OrganizationId { get; init; }
    public OrganizationEntity Organization { get; init; } = null!;
    public required int Month { get; set; }
    public required int Year { get; set; }
    public required string Description { get; set; }
    public required decimal Amount { get; set; }

    public required Guid? AppliedToInvoiceId { get; set; }
    public InvoiceEntity? AppliedToInvoice { get; init; }

    public required Guid? CreatedByUserId { get; init; }
    public ApplicationUserEntity? CreatedByUser { get; init; }

    [NotMapped]
    public Guid AuditEntityId => InvoiceAdjustmentId;

    [NotMapped]
    public Guid? AuditOrganizationId => OrganizationId;

    [NotMapped]
    public AuditableEntityType AuditEntityType => AuditableEntityType.InvoiceAdjustment;

    [NotMapped]
    public IReadOnlyCollection<AuditedProperty> AuditedProperties => AuditedPropertyRegistry.InvoiceAdjustment;
}
