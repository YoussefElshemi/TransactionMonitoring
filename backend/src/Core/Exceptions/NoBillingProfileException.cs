using System.Net;
using Core.ValueObjects;

namespace Core.Exceptions;

public class NoBillingProfileException(OrganizationId organizationId)
    : DomainException($"Organization '{organizationId}' has no billing profile. Record its billing details before invoicing it.")
{
    public OrganizationId OrganizationId { get; } = organizationId;

    public override HttpStatusCode GetStatusCode()
    {
        return HttpStatusCode.Conflict;
    }

    public override string GetTitle()
    {
        return "No billing profile";
    }
}
