namespace Infrastructure.Entities;

public record CreditNoteLineEntity
{
    public required Guid CreditNoteLineId { get; init; }
    public required Guid CreditNoteId { get; init; }
    public CreditNoteEntity CreditNote { get; init; } = null!;
    public required string Description { get; init; }
    public required int Quantity { get; init; }
    public required decimal UnitAmount { get; init; }
    public required decimal Amount { get; init; }
    public required int SortOrder { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}
