namespace Presentation.Models.CreditNotes;

public record CreditNoteLineResponseDto
{
    public required Guid CreditNoteLineId { get; init; }
    public required string Description { get; init; }
    public required int Quantity { get; init; }
    public required decimal UnitAmount { get; init; }

    public required decimal Amount { get; init; }

    public required int SortOrder { get; init; }
}
