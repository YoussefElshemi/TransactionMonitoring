using Core.Enums;
using Core.Models.BaseModels;
using Core.ValueObjects;

namespace Core.Models.Accounts;

public record AccountEntry : BaseModel
{
    public required AccountEntryId AccountEntryId { get; init; }
    public required OrganizationId OrganizationId { get; init; }

    public required Currency Currency { get; init; }

    public required EntryAmount Amount { get; init; }
    public required AccountEntryReason Reason { get; init; }

    public required PaymentId? PaymentId { get; init; }
    public required InvoiceId? InvoiceId { get; init; }
    public required CreditNoteId? CreditNoteId { get; init; }

    public required EntryDescription Description { get; init; }

    public required ApplicationUserId? CreatedByUserId { get; init; }
}
