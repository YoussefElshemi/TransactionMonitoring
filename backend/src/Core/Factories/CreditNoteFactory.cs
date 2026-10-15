using Core.Enums;
using Core.Models.CreditNotes;
using Core.Models.Invoices;
using Core.ValueObjects;

namespace Core.Factories;

public static class CreditNoteFactory
{
    private const int MoneyDecimals = 2;

    public static CreditNote Create(
        CreateCreditNoteRequest createCreditNoteRequest,
        Invoice invoice,
        CreditNoteId creditNoteId,
        CreditNoteNumber creditNoteNumber,
        NetAmount netAmount,
        bool mirrorLines,
        TimeProvider timeProvider)
    {
        var now = timeProvider.GetUtcNow();

        var lines = mirrorLines
            ? MirrorLines(invoice, creditNoteId, now)
            : PartialLine(invoice, creditNoteId, netAmount, now);

        var taxAmount = new TaxAmount(Round(netAmount * invoice.TaxRate / 100m));

        return new CreditNote
        {
            CreditNoteId = creditNoteId,
            CreditNoteNumber = creditNoteNumber,
            InvoiceId = invoice.InvoiceId,
            Invoice = null,
            OrganizationId = invoice.OrganizationId,
            BillingEntityId = invoice.BillingEntityId!.Value,
            Reason = createCreditNoteRequest.Reason,
            Currency = invoice.Currency,
            NetAmount = netAmount,
            TaxTreatment = invoice.TaxTreatment,
            TaxRate = invoice.TaxRate,
            TaxAmount = taxAmount,
            Amount = new InvoiceAmount(netAmount + taxAmount),
            IssuedAt = new IssuedAt(now),
            CreatedByUserId = createCreditNoteRequest.CreatedByUserId,

            Locale = invoice.Locale,
            Lines = lines,
            CreatedAt = new CreatedAt(now),
            UpdatedAt = new UpdatedAt(now)
        };
    }

    private static List<CreditNoteLine> MirrorLines(Invoice invoice, CreditNoteId creditNoteId, DateTimeOffset now)
    {
        return invoice.Lines
            .Where(x => x.InvoiceLineType != InvoiceLineType.Tax)
            .OrderBy(x => (int)x.SortOrder)
            .Select((line, index) => new CreditNoteLine
            {
                CreditNoteLineId = new CreditNoteLineId(Guid.NewGuid()),
                CreditNoteId = creditNoteId,
                Description = line.Description,
                Quantity = line.Quantity,
                UnitAmount = line.UnitAmount,
                Amount = line.Amount,
                SortOrder = new SortOrder(index),
                CreatedAt = new CreatedAt(now)
            })
            .ToList();
    }

    private static List<CreditNoteLine> PartialLine(Invoice invoice, CreditNoteId creditNoteId, NetAmount netAmount, DateTimeOffset now)
    {
        var description = invoice.InvoiceNumber.HasValue
            ? "Credit against invoice " + invoice.InvoiceNumber.Value
            : "Credit against invoice " + invoice.InvoiceId;

        return
        [
            new CreditNoteLine
            {
                CreditNoteLineId = new CreditNoteLineId(Guid.NewGuid()),
                CreditNoteId = creditNoteId,
                Description = new InvoiceLineDescription(description),
                Quantity = new Quantity(1),
                UnitAmount = new UnitAmount(netAmount),
                Amount = new LineAmount(netAmount),
                SortOrder = new SortOrder(0),
                CreatedAt = new CreatedAt(now)
            }
        ];
    }

    private static decimal Round(decimal value)
    {
        return Math.Round(value, MoneyDecimals, MidpointRounding.AwayFromZero);
    }
}
