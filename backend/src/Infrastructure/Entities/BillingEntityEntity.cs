using System.ComponentModel.DataAnnotations.Schema;
using Core.Enums;
using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record BillingEntityEntity : DeletableEntity, IAuditable
{
    public required Guid BillingEntityId { get; init; }
    public required string LegalName { get; set; }
    public required string Address { get; set; }
    public required string CountryCode { get; set; }
    public required string? TaxIdentifier { get; set; }

    public required string InvoiceNumberPrefix { get; init; }
    public required string CreditNoteNumberPrefix { get; init; }

    public required int CurrencyId { get; set; }
    public CurrencyEntity Currency { get; init; } = null!;
    public required bool IsEnabled { get; set; }
    public required Guid? CreatedByUserId { get; init; }
    public ApplicationUserEntity? CreatedByUser { get; init; }
    public ICollection<BillingProfileEntity> BillingProfiles { get; init; } = [];
    public ICollection<InvoiceEntity> Invoices { get; init; } = [];
    public ICollection<CreditNoteEntity> CreditNotes { get; init; } = [];
    public ICollection<InvoiceNumberSequenceEntity> NumberSequences { get; init; } = [];

    [NotMapped]
    public Guid AuditEntityId => BillingEntityId;

    [NotMapped]
    public Guid? AuditOrganizationId => null;

    [NotMapped]
    public AuditableEntityType AuditEntityType => AuditableEntityType.BillingEntity;

    [NotMapped]
    public IReadOnlyCollection<AuditedProperty> AuditedProperties => AuditedPropertyRegistry.BillingEntity;
}
