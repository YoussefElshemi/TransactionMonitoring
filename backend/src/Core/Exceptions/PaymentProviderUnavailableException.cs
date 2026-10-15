using System.Net;

namespace Core.Exceptions;

public class PaymentProviderUnavailableException()
    : DomainException("No payment provider is configured on this installation, so nothing can be collected.")
{
    public override HttpStatusCode GetStatusCode()
    {
        return HttpStatusCode.ServiceUnavailable;
    }

    public override string GetTitle()
    {
        return "Payment provider unavailable";
    }
}
