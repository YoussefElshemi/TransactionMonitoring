using Core.Models.CreditNotes;
using Core.ValueObjects;
using Infrastructure.Entities;

namespace Infrastructure.Mappers;

public static class CreditNoteLineMapper
{
    public static CreditNoteLine ToModel(this CreditNoteLineEntity creditNoteLineEntity)
    {
        return new CreditNoteLine
        {
            CreditNoteLineId = new CreditNoteLineId(creditNoteLineEntity.CreditNoteLineId),
            CreditNoteId = new CreditNoteId(creditNoteLineEntity.CreditNoteId),
            Description = new InvoiceLineDescription(creditNoteLineEntity.Description),
            Quantity = new Quantity(creditNoteLineEntity.Quantity),
            UnitAmount = new UnitAmount(creditNoteLineEntity.UnitAmount),
            Amount = new LineAmount(creditNoteLineEntity.Amount),
            SortOrder = new SortOrder(creditNoteLineEntity.SortOrder),
            CreatedAt = new CreatedAt(creditNoteLineEntity.CreatedAt)
        };
    }

    public static CreditNoteLineEntity ToEntity(this CreditNoteLine creditNoteLine)
    {
        return new CreditNoteLineEntity
        {
            CreditNoteLineId = creditNoteLine.CreditNoteLineId,
            CreditNoteId = creditNoteLine.CreditNoteId,
            Description = creditNoteLine.Description,
            Quantity = creditNoteLine.Quantity,
            UnitAmount = creditNoteLine.UnitAmount,
            Amount = creditNoteLine.Amount,
            SortOrder = creditNoteLine.SortOrder,
            CreatedAt = creditNoteLine.CreatedAt
        };
    }
}
