namespace Presentation.Models.CreditNotes;

public record CreditNoteResponseDto
{
    public required Guid CreditNoteId { get; init; }
    public required string CreditNoteNumber { get; init; }
    public required Guid InvoiceId { get; init; }
    public required string? InvoiceNumber { get; init; }
    public required Guid OrganizationId { get; init; }
    public required Guid BillingEntityId { get; init; }
    public required string Reason { get; init; }
    public required string Currency { get; init; }
    public required decimal NetAmount { get; init; }

    public required string TaxTreatment { get; init; }
    public required decimal TaxRate { get; init; }
    public required decimal TaxAmount { get; init; }

    public required decimal Amount { get; init; }
    public required DateTimeOffset IssuedAt { get; init; }
    public required Guid? CreatedByUserId { get; init; }
    public required List<CreditNoteLineResponseDto> Lines { get; init; }
}
