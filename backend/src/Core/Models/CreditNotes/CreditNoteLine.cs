using Core.ValueObjects;

namespace Core.Models.CreditNotes;

public record CreditNoteLine
{
    public required CreditNoteLineId CreditNoteLineId { get; init; }
    public required CreditNoteId CreditNoteId { get; init; }
    public required InvoiceLineDescription Description { get; init; }
    public required Quantity Quantity { get; init; }
    public required UnitAmount UnitAmount { get; init; }

    // Positive: the document type already carries the sign.
    public required LineAmount Amount { get; init; }

    public required SortOrder SortOrder { get; init; }
    public required CreatedAt CreatedAt { get; init; }
}
