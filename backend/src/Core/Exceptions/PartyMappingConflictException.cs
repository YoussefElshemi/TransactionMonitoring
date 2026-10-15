using System.Net;
using Core.ValueObjects;

namespace Core.Exceptions;

public class PartyMappingConflictException(OrganizationId organizationId, Exception innerException)
    : DomainException($"The party mapping for organisation '{organizationId}' was changed concurrently.", innerException)
{
    public OrganizationId OrganizationId { get; } = organizationId;

    public override HttpStatusCode GetStatusCode()
    {
        return HttpStatusCode.Conflict;
    }

    public override string GetTitle()
    {
        return "Party mapping was changed concurrently";
    }

    public override object GetExtensions()
    {
        return new
        {
            OrganizationId = OrganizationId.ToString()
        };
    }
}
