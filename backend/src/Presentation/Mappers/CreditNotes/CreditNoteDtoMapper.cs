using Core.Helpers;
using Core.Models.CreditNotes;
using Core.ValueObjects;
using Presentation.Models.CreditNotes;

namespace Presentation.Mappers.CreditNotes;

public static class CreditNoteDtoMapper
{
    public static CreateCreditNoteRequest ToModel(this CreateCreditNoteRequestDto dto)
    {
        return new CreateCreditNoteRequest
        {
            OrganizationId = new OrganizationId(dto.OrganizationId),
            InvoiceId = new InvoiceId(dto.InvoiceId),
            Reason = new CreditNoteReason(dto.Body.Reason),
            NetAmount = dto.Body.NetAmount.HasValue ? new NetAmount(dto.Body.NetAmount.Value) : null,
            CreatedByUserId = dto.CreatedByUserId.HasValue ? new ApplicationUserId(dto.CreatedByUserId.Value) : null
        };
    }

    public static ListCreditNoteRequest ToModel(this ListCreditNoteRequestDto dto)
    {
        return new ListCreditNoteRequest
        {
            OrganizationId = new OrganizationId(dto.OrganizationId),
            InvoiceId = dto.InvoiceId.HasValue ? new InvoiceId(dto.InvoiceId.Value) : null,
            PageNumber = new PageNumber(dto.PageNumber),
            PageSize = new PageSize(dto.PageSize),
            FromDate = new DateRange(dto.FromDate),
            ToDate = new DateRange(dto.ToDate)
        };
    }

    public static CreditNoteResponseDto ToDto(this CreditNote creditNote)
    {
        return new CreditNoteResponseDto
        {
            CreditNoteId = creditNote.CreditNoteId,
            CreditNoteNumber = creditNote.CreditNoteNumber,
            InvoiceId = creditNote.InvoiceId,
            InvoiceNumber = creditNote.Invoice?.InvoiceNumber.HasValue == true ? creditNote.Invoice.InvoiceNumber.Value : null,
            OrganizationId = creditNote.OrganizationId,
            BillingEntityId = creditNote.BillingEntityId,
            Reason = creditNote.Reason,
            Currency = CurrencyFormatter.Code(creditNote.Currency),
            NetAmount = creditNote.NetAmount,
            TaxTreatment = creditNote.TaxTreatment.ToString(),
            TaxRate = creditNote.TaxRate,
            TaxAmount = creditNote.TaxAmount,
            Amount = creditNote.Amount,
            IssuedAt = creditNote.IssuedAt,
            CreatedByUserId = creditNote.CreatedByUserId.HasValue ? creditNote.CreatedByUserId.Value : null,
            Lines = creditNote.Lines.ConvertAll(x => x.ToDto())
        };
    }

    public static CreditNoteLineResponseDto ToDto(this CreditNoteLine creditNoteLine)
    {
        return new CreditNoteLineResponseDto
        {
            CreditNoteLineId = creditNoteLine.CreditNoteLineId,
            Description = creditNoteLine.Description,
            Quantity = creditNoteLine.Quantity,
            UnitAmount = creditNoteLine.UnitAmount,
            Amount = creditNoteLine.Amount,
            SortOrder = creditNoteLine.SortOrder
        };
    }
}
