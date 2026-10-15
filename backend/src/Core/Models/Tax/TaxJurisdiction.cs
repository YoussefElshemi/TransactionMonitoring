using Core.ValueObjects;

namespace Core.Models.Tax;

public record TaxJurisdiction
{
    public required CountryCode CountryCode { get; init; }
    public required LegalName Name { get; init; }
    public required IsEuMember IsEuMember { get; init; }
    public required TaxRate StandardRate { get; init; }
    public required TaxIdentifierPattern TaxIdentifierPattern { get; init; }
}
