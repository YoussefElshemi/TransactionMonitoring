namespace Presentation.Models.Accounts;

public record AccountEntryResponseDto
{
    public required Guid AccountEntryId { get; init; }
    public required Guid OrganizationId { get; init; }
    public required string Currency { get; init; }
    public required decimal Amount { get; init; }
    public required string Reason { get; init; }
    public required Guid? PaymentId { get; init; }
    public required Guid? InvoiceId { get; init; }
    public required Guid? CreditNoteId { get; init; }
    public required string Description { get; init; }
    public required Guid? CreatedByUserId { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}
