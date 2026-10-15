using System.Net;
using Core.ValueObjects;

namespace Core.Exceptions;

public class NoPaymentMethodException(OrganizationId organizationId)
    : DomainException($"Organization '{organizationId}' has no payment method on file. Add one before collecting against it.")
{
    public OrganizationId OrganizationId { get; } = organizationId;

    public override HttpStatusCode GetStatusCode()
    {
        return HttpStatusCode.Conflict;
    }

    public override string GetTitle()
    {
        return "No payment method";
    }
}
