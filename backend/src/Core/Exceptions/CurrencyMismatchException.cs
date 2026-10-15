using System.Net;
using Core.Enums;
using Core.ValueObjects;

namespace Core.Exceptions;

public class CurrencyMismatchException(InvoiceId invoiceId, Currency invoiceCurrency, Currency paymentCurrency)
    : DomainException($"Invoice '{invoiceId}' is in {invoiceCurrency} and the money offered is in {paymentCurrency}. Nothing here converts between them.")
{
    public InvoiceId InvoiceId { get; } = invoiceId;
    public Currency InvoiceCurrency { get; } = invoiceCurrency;
    public Currency PaymentCurrency { get; } = paymentCurrency;

    public override HttpStatusCode GetStatusCode()
    {
        return HttpStatusCode.Conflict;
    }

    public override string GetTitle()
    {
        return "Currency mismatch";
    }
}
