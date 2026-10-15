using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record InvoiceEntity : ProcessableEntity
{
    public required Guid InvoiceId { get; init; }

    public required string? InvoiceNumber { get; set; }

    public required int InvoiceStatusId { get; set; }
    public InvoiceStatusEntity InvoiceStatus { get; init; } = null!;
    public required Guid? BillingEntityId { get; init; }
    public BillingEntityEntity? BillingEntity { get; init; }
    public required Guid? PlanVersionId { get; init; }
    public PlanVersionEntity? PlanVersion { get; init; }
    public required decimal CostPerRequest { get; init; }

    public required string? SupplierName { get; init; }
    public required string? SupplierAddress { get; init; }
    public required string? SupplierCountryCode { get; init; }
    public required string? SupplierTaxIdentifier { get; init; }
    public required string? CustomerName { get; init; }
    public required string? CustomerAddress { get; init; }
    public required string? CustomerCountryCode { get; init; }
    public required string? CustomerTaxIdentifier { get; init; }
    public required string? CustomerEmail { get; init; }

    public required int CurrencyId { get; init; }
    public CurrencyEntity Currency { get; init; } = null!;
    public required int PlanCurrencyId { get; init; }
    public CurrencyEntity PlanCurrency { get; init; } = null!;

    public required decimal? ConversionRate { get; init; }

    public required int TaxTreatmentId { get; init; }
    public TaxTreatmentEntity TaxTreatment { get; init; } = null!;
    public required decimal TaxRate { get; init; }
    public required decimal TaxAmount { get; init; }
    public required decimal NetAmount { get; init; }

    public required decimal Amount { get; init; }
    public required int Month { get; init; }
    public required int Year { get; init; }
    public required DateTimeOffset? IssuedAt { get; set; }
    public required DateTimeOffset? DueAt { get; set; }
    public required int PaymentTerms { get; init; }

    public required Guid OrganizationId { get; init; }
    public OrganizationEntity Organization { get; init; } = null!;
    public required Guid? CreatedByUserId { get; init; }
    public ApplicationUserEntity? CreatedByUser { get; init; }
    public ICollection<InvoiceLineEntity> Lines { get; init; } = [];

    public required string? Locale { get; init; }
    public ICollection<InvoiceAdjustmentEntity> Adjustments { get; init; } = [];
    public ICollection<CreditNoteEntity> CreditNotes { get; init; } = [];
}