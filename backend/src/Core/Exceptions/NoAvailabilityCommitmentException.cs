using System.Net;
using Core.ValueObjects;

namespace Core.Exceptions;

public class NoAvailabilityCommitmentException(OrganizationId organizationId, Month month, Year year)
    : DomainException($"Organization '{organizationId}' had no availability commitment in force during {month}/{year}.")
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
        return "No availability commitment";
    }
}
