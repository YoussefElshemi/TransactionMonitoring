using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

// Not a database sequence: those hand out numbers outside the transaction, so a rollback would leave a gap.
public record InvoiceNumberSequenceEntity : BaseEntity
{
    public required Guid BillingEntityId { get; init; }
    public BillingEntityEntity BillingEntity { get; init; } = null!;
    public required int Year { get; init; }
    public required int LastInvoiceNumber { get; set; }
    public required int LastCreditNoteNumber { get; set; }

    public required int LastReceiptNumber { get; set; }
}
