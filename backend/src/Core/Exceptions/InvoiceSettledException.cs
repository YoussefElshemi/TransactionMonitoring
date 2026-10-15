using System.Net;
using Core.ValueObjects;

namespace Core.Exceptions;

public class InvoiceSettledException(InvoiceId invoiceId, InvoiceNumber? invoiceNumber)
    : DomainException($"Invoice '{invoiceNumber?.ToString() ?? invoiceId.ToString()}' has nothing outstanding.")
{
    public InvoiceId InvoiceId { get; } = invoiceId;
    public InvoiceNumber? InvoiceNumber { get; } = invoiceNumber;

    public override HttpStatusCode GetStatusCode()
    {
        return HttpStatusCode.Conflict;
    }

    public override string GetTitle()
    {
        return "Invoice settled";
    }
}
