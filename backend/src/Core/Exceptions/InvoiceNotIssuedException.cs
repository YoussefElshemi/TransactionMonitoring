using System.Net;
using Core.ValueObjects;

namespace Core.Exceptions;

public class InvoiceNotIssuedException(InvoiceId invoiceId)
    : DomainException($"Invoice '{invoiceId}' is still a draft. Issue it before acting on it.")
{
    public InvoiceId InvoiceId { get; } = invoiceId;

    public override HttpStatusCode GetStatusCode()
    {
        return HttpStatusCode.Conflict;
    }

    public override string GetTitle()
    {
        return "Invoice not issued";
    }
}
