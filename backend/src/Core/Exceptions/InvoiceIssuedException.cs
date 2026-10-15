using System.Net;
using Core.ValueObjects;

namespace Core.Exceptions;

public class InvoiceIssuedException(InvoiceId invoiceId, InvoiceNumber? invoiceNumber)
    : DomainException($"Invoice '{invoiceNumber?.ToString() ?? invoiceId.ToString()}' has been issued and cannot be changed. Raise a credit note against it instead.")
{
    public InvoiceId InvoiceId { get; } = invoiceId;
    public InvoiceNumber? InvoiceNumber { get; } = invoiceNumber;

    public override HttpStatusCode GetStatusCode()
    {
        return HttpStatusCode.Conflict;
    }

    public override string GetTitle()
    {
        return "Invoice already issued";
    }
}
