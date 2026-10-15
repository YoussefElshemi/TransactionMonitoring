using System.Net;
using Core.ValueObjects;

namespace Core.Exceptions;

public class InvalidTaxIdentifierException(CountryCode countryCode, TaxIdentifier taxIdentifier)
    : DomainException($"'{taxIdentifier}' is not the shape a tax registration takes in {countryCode}.")
{
    public CountryCode CountryCode { get; } = countryCode;
    public TaxIdentifier TaxIdentifier { get; } = taxIdentifier;

    public override HttpStatusCode GetStatusCode()
    {
        return HttpStatusCode.UnprocessableEntity;
    }

    public override string GetTitle()
    {
        return "Invalid tax identifier";
    }
}
