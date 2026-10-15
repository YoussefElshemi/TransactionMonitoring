using System.ComponentModel.DataAnnotations.Schema;
using Core.Enums;
using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record CreditNoteEntity : BaseEntity, IAuditable
{
    public required Guid CreditNoteId { get; init; }
    public required string CreditNoteNumber { get; init; }
    public required Guid InvoiceId { get; init; }
    public InvoiceEntity Invoice { get; init; } = null!;
    public required Guid OrganizationId { get; init; }
    public OrganizationEntity Organization { get; init; } = null!;
    public required Guid BillingEntityId { get; init; }
    public BillingEntityEntity BillingEntity { get; init; } = null!;
    public required string Reason { get; init; }
    public required int CurrencyId { get; init; }
    public CurrencyEntity Currency { get; init; } = null!;
    public required decimal NetAmount { get; init; }
    public required int TaxTreatmentId { get; init; }
    public TaxTreatmentEntity TaxTreatment { get; init; } = null!;
    public required decimal TaxRate { get; init; }
    public required decimal TaxAmount { get; init; }
    public required decimal Amount { get; init; }
    public required DateTimeOffset IssuedAt { get; init; }
    public required Guid? CreatedByUserId { get; init; }
    public ApplicationUserEntity? CreatedByUser { get; init; }
    public ICollection<CreditNoteLineEntity> Lines { get; init; } = [];

    public required string? Locale { get; init; }

    [NotMapped]
    public Guid AuditEntityId => CreditNoteId;

    [NotMapped]
    public Guid? AuditOrganizationId => OrganizationId;

    [NotMapped]
    public AuditableEntityType AuditEntityType => AuditableEntityType.CreditNote;

    [NotMapped]
    public IReadOnlyCollection<AuditedProperty> AuditedProperties => AuditedPropertyRegistry.CreditNote;
}
