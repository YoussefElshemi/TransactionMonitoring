using System.Net;
using Core.ValueObjects;

namespace Core.Exceptions;

public class BillingEntityInUseException(BillingEntityId billingEntityId)
    : DomainException($"Billing entity '{billingEntityId}' has issued documents or has customers pointed at it. Disable it instead of removing it.")
{
    public BillingEntityId BillingEntityId { get; } = billingEntityId;

    public override HttpStatusCode GetStatusCode()
    {
        return HttpStatusCode.Conflict;
    }

    public override string GetTitle()
    {
        return "Billing entity in use";
    }
}
