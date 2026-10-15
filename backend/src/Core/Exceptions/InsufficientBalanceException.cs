using System.Net;
using Core.Enums;
using Core.ValueObjects;

namespace Core.Exceptions;

public class InsufficientBalanceException(OrganizationId organizationId, Currency currency, decimal available, decimal requested)
    : DomainException($"Organization '{organizationId}' has {available} {currency} on account and {requested} was requested.")
{
    public OrganizationId OrganizationId { get; } = organizationId;
    public Currency Currency { get; } = currency;
    public decimal Available { get; } = available;
    public decimal Requested { get; } = requested;

    public override HttpStatusCode GetStatusCode()
    {
        return HttpStatusCode.Conflict;
    }

    public override string GetTitle()
    {
        return "Insufficient account balance";
    }
}
