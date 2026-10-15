using System.Net;
using Core.ValueObjects;

namespace Core.Exceptions;

public class ServiceCreditAlreadyIssuedException(OrganizationId organizationId, Month month, Year year)
    : DomainException($"Organization '{organizationId}' has already been measured for {month}/{year}.")
{
    public OrganizationId OrganizationId { get; } = organizationId;
    public Month Month { get; } = month;
    public Year Year { get; } = year;

    public override HttpStatusCode GetStatusCode()
    {
        return HttpStatusCode.Conflict;
    }

    public override string GetTitle()
    {
        return "Service credit already issued";
    }
}
