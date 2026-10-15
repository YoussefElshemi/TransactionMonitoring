using Core.ValueObjects;

namespace Core.Models.Billing;

public record DocumentParty
{
    public required LegalName LegalName { get; init; }
    public required PostalAddress Address { get; init; }

    public required CountryCode CountryCode { get; init; }

    public required TaxIdentifier? TaxIdentifier { get; init; }

    public required Email? Email { get; init; }
}
