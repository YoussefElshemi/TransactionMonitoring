using Core.ValueObjects;

namespace Core.Models.CreditNotes;

public record CreateCreditNoteRequest
{
    public required OrganizationId OrganizationId { get; init; }
    public required InvoiceId InvoiceId { get; init; }
    public required CreditNoteReason Reason { get; init; }

    public required NetAmount? NetAmount { get; init; }

    public required ApplicationUserId? CreatedByUserId { get; init; }
}
