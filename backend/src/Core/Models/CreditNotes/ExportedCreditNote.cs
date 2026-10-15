namespace Core.Models.CreditNotes;

public record ExportedCreditNote
{
    public required CreditNote CreditNote { get; init; }
    public required byte[] Content { get; init; }
}
