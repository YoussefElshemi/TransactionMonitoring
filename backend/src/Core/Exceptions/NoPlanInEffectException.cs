using System.Net;
using Core.ValueObjects;

namespace Core.Exceptions;

public class NoPlanInEffectException(OrganizationId organizationId, Month month, Year year)
    : DomainException($"Organization '{organizationId}' was on no plan during {month}/{year}. Subscribe it to a plan before invoicing that period.")
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
        return "No plan in effect";
    }
}
