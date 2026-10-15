using System.Net;
using Core.ValueObjects;

namespace Core.Exceptions;

public class TransactionSchemaConflictException(OrganizationId organizationId, Exception innerException)
    : DomainException($"The transaction schema for organisation '{organizationId}' was changed concurrently.", innerException)
{
    public OrganizationId OrganizationId { get; } = organizationId;

    public override HttpStatusCode GetStatusCode()
    {
        return HttpStatusCode.Conflict;
    }

    public override string GetTitle()
    {
        return "Transaction schema was changed concurrently";
    }

    public override object GetExtensions()
    {
        return new
        {
            OrganizationId = OrganizationId.ToString()
        };
    }
}
