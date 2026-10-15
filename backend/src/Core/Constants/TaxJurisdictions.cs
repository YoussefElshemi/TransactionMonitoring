using System.Collections.Immutable;
using Core.Models.Tax;
using Core.ValueObjects;

namespace Core.Constants;

public static class TaxJurisdictions
{
    public static readonly ImmutableDictionary<string, TaxJurisdiction> ByCountryCode = new[]
    {
        Create("GB", "United Kingdom", false, 20m, @"^GB(\d{9}|\d{12}|GD\d{3}|HA\d{3})$"),

        Create("AT", "Austria", true, 20m, @"^ATU\d{8}$"),
        Create("BE", "Belgium", true, 21m, @"^BE0?\d{9,10}$"),
        Create("BG", "Bulgaria", true, 20m, @"^BG\d{9,10}$"),
        Create("CY", "Cyprus", true, 19m, @"^CY\d{8}[A-Z]$"),
        Create("CZ", "Czechia", true, 21m, @"^CZ\d{8,10}$"),
        Create("DE", "Germany", true, 19m, @"^DE\d{9}$"),
        Create("DK", "Denmark", true, 25m, @"^DK\d{8}$"),
        Create("EE", "Estonia", true, 22m, @"^EE\d{9}$"),
        Create("ES", "Spain", true, 21m, @"^ES[A-Z0-9]\d{7}[A-Z0-9]$"),
        Create("FI", "Finland", true, 25.5m, @"^FI\d{8}$"),
        Create("FR", "France", true, 20m, @"^FR[A-Z0-9]{2}\d{9}$"),
        Create("GR", "Greece", true, 24m, @"^EL\d{9}$"),
        Create("HR", "Croatia", true, 25m, @"^HR\d{11}$"),
        Create("HU", "Hungary", true, 27m, @"^HU\d{8}$"),
        Create("IE", "Ireland", true, 23m, @"^IE(\d{7}[A-Z]{1,2}|\d[A-Z]\d{5}[A-Z])$"),
        Create("IT", "Italy", true, 22m, @"^IT\d{11}$"),
        Create("LT", "Lithuania", true, 21m, @"^LT(\d{9}|\d{12})$"),
        Create("LU", "Luxembourg", true, 17m, @"^LU\d{8}$"),
        Create("LV", "Latvia", true, 21m, @"^LV\d{11}$"),
        Create("MT", "Malta", true, 18m, @"^MT\d{8}$"),
        Create("NL", "Netherlands", true, 21m, @"^NL\d{9}B\d{2}$"),
        Create("PL", "Poland", true, 23m, @"^PL\d{10}$"),
        Create("PT", "Portugal", true, 23m, @"^PT\d{9}$"),
        Create("RO", "Romania", true, 19m, @"^RO\d{2,10}$"),
        Create("SE", "Sweden", true, 25m, @"^SE\d{12}$"),
        Create("SI", "Slovenia", true, 22m, @"^SI\d{8}$"),
        Create("SK", "Slovakia", true, 23m, @"^SK\d{10}$")
    }.ToImmutableDictionary(x => (string)x.CountryCode);

    public static TaxJurisdiction? Find(CountryCode countryCode)
    {
        return ByCountryCode.TryGetValue(countryCode, out var taxJurisdiction) ? taxJurisdiction : null;
    }

    public static string RegistrationPrefix(CountryCode countryCode)
    {
        return countryCode == "GR" ? "EL" : countryCode;
    }

    private static TaxJurisdiction Create(string countryCode, string name, bool isEuMember, decimal standardRate, string pattern)
    {
        return new TaxJurisdiction
        {
            CountryCode = new CountryCode(countryCode),
            Name = new LegalName(name),
            IsEuMember = new IsEuMember(isEuMember),
            StandardRate = new TaxRate(standardRate),
            TaxIdentifierPattern = new TaxIdentifierPattern(pattern)
        };
    }
}
