using System.Net;
using Core.ValueObjects;

namespace Core.Exceptions;

public class CreditExceedsInvoiceException(InvoiceId invoiceId, NetAmount requested, NetAmount remaining)
    : DomainException($"Crediting {requested} against invoice '{invoiceId}' exceeds the {remaining} still uncredited on it.")
{
    public InvoiceId InvoiceId { get; } = invoiceId;
    public NetAmount Requested { get; } = requested;
    public NetAmount Remaining { get; } = remaining;

    public override HttpStatusCode GetStatusCode()
    {
        return HttpStatusCode.Conflict;
    }

    public override string GetTitle()
    {
        return "Credit exceeds invoice";
    }
}
