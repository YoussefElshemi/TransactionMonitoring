using System.Net;
using Core.ValueObjects;

namespace Core.Exceptions;

public class InvoiceAdjustmentAppliedException(InvoiceId invoiceId)
    : DomainException($"The adjustment has already been carried onto invoice '{invoiceId}'. Reissue that invoice rather than deleting the adjustment.")
{
    public InvoiceId InvoiceId { get; } = invoiceId;

    public override HttpStatusCode GetStatusCode()
    {
        return HttpStatusCode.Conflict;
    }

    public override string GetTitle()
    {
        return "Adjustment already applied";
    }
}
