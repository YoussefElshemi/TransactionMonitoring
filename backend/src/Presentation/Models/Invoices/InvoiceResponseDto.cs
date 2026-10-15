namespace Presentation.Models.Invoices;

public record InvoiceResponseDto
{
    public required Guid InvoiceId { get; init; }

    public required string? InvoiceNumber { get; init; }

    public required string InvoiceStatus { get; init; }
    public required Guid? BillingEntityId { get; init; }

    public required Guid? PlanVersionId { get; init; }
    public required int? PlanVersionNumber { get; init; }
    public required decimal CostPerRequest { get; init; }

    public required InvoicePartyResponseDto? Supplier { get; init; }
    public required InvoicePartyResponseDto? Customer { get; init; }

    public required string Currency { get; init; }
    public required string PlanCurrency { get; init; }
    public required decimal? ConversionRate { get; init; }

    public required string TaxTreatment { get; init; }
    public required decimal TaxRate { get; init; }
    public required decimal TaxAmount { get; init; }

    public required decimal NetAmount { get; init; }
    public required decimal Amount { get; init; }

    public required DateTimeOffset? IssuedAt { get; init; }
    public required DateTimeOffset? DueAt { get; init; }
    public required int PaymentTerms { get; init; }

    public required int Month { get; init; }
    public required int Year { get; init; }
    public required Guid OrganizationId { get; init; }
    public required Guid? CreatedByUserId { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
    public required bool IsProcessed { get; init; }
    public required DateTimeOffset? ProcessedAt { get; init; }

    public required List<InvoiceLineResponseDto> Lines { get; init; }
}
