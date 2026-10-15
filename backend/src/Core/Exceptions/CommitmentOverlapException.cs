using System.Net;
using Core.ValueObjects;

namespace Core.Exceptions;

public class CommitmentOverlapException(OrganizationId organizationId)
    : DomainException($"Organization '{organizationId}' already has a commitment covering part of that term. End the existing one before opening another.")
{
    public OrganizationId OrganizationId { get; } = organizationId;

    public override HttpStatusCode GetStatusCode()
    {
        return HttpStatusCode.Conflict;
    }

    public override string GetTitle()
    {
        return "Overlapping commitment";
    }
}
