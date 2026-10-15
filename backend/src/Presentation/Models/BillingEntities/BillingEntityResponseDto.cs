namespace Presentation.Models.BillingEntities;

public record BillingEntityResponseDto
{
    public required Guid BillingEntityId { get; init; }
    public required string LegalName { get; init; }
    public required string Address { get; init; }
    public required string CountryCode { get; init; }
    public required string? TaxIdentifier { get; init; }
    public required string InvoiceNumberPrefix { get; init; }
    public required string CreditNoteNumberPrefix { get; init; }
    public required string Currency { get; init; }
    public required bool IsEnabled { get; init; }
    public required Guid? CreatedByUserId { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
}
