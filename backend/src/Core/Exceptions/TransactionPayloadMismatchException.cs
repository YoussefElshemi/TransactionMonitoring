using System.Net;
using Core.ValueObjects;

namespace Core.Exceptions;

public class TransactionPayloadMismatchException(ExternalReference externalReference)
    : DomainException(
        $"The transaction reference '{externalReference}' already names a payment with a different payload. " +
        "Send the same payload to add to that payment, or a new reference for a new one.")
{
    public ExternalReference ExternalReference { get; } = externalReference;

    public override HttpStatusCode GetStatusCode()
    {
        return HttpStatusCode.BadRequest;
    }

    public override string GetTitle()
    {
        return "Transaction reference does not match its payload";
    }

    public override object GetExtensions()
    {
        return new
        {
            TransactionReference = ExternalReference.ToString()
        };
    }
}
