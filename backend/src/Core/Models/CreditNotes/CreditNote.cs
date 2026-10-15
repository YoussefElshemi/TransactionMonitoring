using Core.Enums;
using Core.Models.BaseModels;
using Core.Models.Invoices;
using Core.ValueObjects;

namespace Core.Models.CreditNotes;

public record CreditNote : BaseModel
{
    public required CreditNoteId CreditNoteId { get; init; }
    public required CreditNoteNumber CreditNoteNumber { get; init; }
    public required InvoiceId InvoiceId { get; init; }
    public required Invoice? Invoice { get; init; }
    public required OrganizationId OrganizationId { get; init; }
    public required BillingEntityId BillingEntityId { get; init; }
    public required CreditNoteReason Reason { get; init; }
    public required Currency Currency { get; init; }

    public required NetAmount NetAmount { get; init; }

    public required TaxTreatment TaxTreatment { get; init; }
    public required TaxRate TaxRate { get; init; }
    public required TaxAmount TaxAmount { get; init; }

    public required InvoiceAmount Amount { get; init; }

    public required IssuedAt IssuedAt { get; init; }

    public required ApplicationUserId? CreatedByUserId { get; init; }
    public required List<CreditNoteLine> Lines { get; init; }

    public required Locale? Locale { get; init; }
}
