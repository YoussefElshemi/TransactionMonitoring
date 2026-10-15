using Core.Enums;
using Core.Models.CreditNotes;
using Core.ValueObjects;
using Infrastructure.Entities;

namespace Infrastructure.Mappers;

public static class CreditNoteMapper
{
    public static CreditNote ToModel(this CreditNoteEntity creditNoteEntity)
    {
        return new CreditNote
        {
            CreditNoteId = new CreditNoteId(creditNoteEntity.CreditNoteId),
            CreditNoteNumber = new CreditNoteNumber(creditNoteEntity.CreditNoteNumber),
            InvoiceId = new InvoiceId(creditNoteEntity.InvoiceId),
            Invoice = creditNoteEntity.Invoice?.ToModel(),
            OrganizationId = new OrganizationId(creditNoteEntity.OrganizationId),
            BillingEntityId = new BillingEntityId(creditNoteEntity.BillingEntityId),
            Reason = new CreditNoteReason(creditNoteEntity.Reason),
            Currency = (Currency)creditNoteEntity.CurrencyId,
            NetAmount = new NetAmount(creditNoteEntity.NetAmount),
            TaxTreatment = (TaxTreatment)creditNoteEntity.TaxTreatmentId,
            TaxRate = new TaxRate(creditNoteEntity.TaxRate),
            TaxAmount = new TaxAmount(creditNoteEntity.TaxAmount),
            Amount = new InvoiceAmount(creditNoteEntity.Amount),
            IssuedAt = new IssuedAt(creditNoteEntity.IssuedAt),
            CreatedByUserId = creditNoteEntity.CreatedByUserId.HasValue ? new ApplicationUserId(creditNoteEntity.CreatedByUserId.Value) : null,
            Locale = creditNoteEntity.Locale is not null ? new Locale(creditNoteEntity.Locale) : null,
            Lines = creditNoteEntity.Lines.OrderBy(x => x.SortOrder).Select(x => x.ToModel()).ToList(),
            CreatedAt = new CreatedAt(creditNoteEntity.CreatedAt),
            UpdatedAt = new UpdatedAt(creditNoteEntity.UpdatedAt)
        };
    }

    public static CreditNoteEntity ToEntity(this CreditNote creditNote)
    {
        return new CreditNoteEntity
        {
            CreditNoteId = creditNote.CreditNoteId,
            CreditNoteNumber = creditNote.CreditNoteNumber,
            InvoiceId = creditNote.InvoiceId,
            OrganizationId = creditNote.OrganizationId,
            BillingEntityId = creditNote.BillingEntityId,
            Reason = creditNote.Reason,
            CurrencyId = (int)creditNote.Currency,
            NetAmount = creditNote.NetAmount,
            TaxTreatmentId = (int)creditNote.TaxTreatment,
            TaxRate = creditNote.TaxRate,
            TaxAmount = creditNote.TaxAmount,
            Amount = creditNote.Amount,
            IssuedAt = creditNote.IssuedAt,
            CreatedByUserId = creditNote.CreatedByUserId.HasValue ? creditNote.CreatedByUserId.Value : null,
            Locale = creditNote.Locale?.ToString(),
            CreatedAt = creditNote.CreatedAt,
            UpdatedAt = creditNote.UpdatedAt
        };
    }
}
