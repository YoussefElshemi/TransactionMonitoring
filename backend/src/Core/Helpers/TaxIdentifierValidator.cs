using System.Text.RegularExpressions;
using Core.Constants;
using Core.ValueObjects;

namespace Core.Helpers;

public static class TaxIdentifierValidator
{
    private static readonly TimeSpan MatchTimeout = TimeSpan.FromMilliseconds(100);

    public static bool IsWellFormed(CountryCode countryCode, TaxIdentifier taxIdentifier)
    {
        var jurisdiction = TaxJurisdictions.Find(countryCode);

        if (jurisdiction is null)
        {
            return false;
        }

        return Regex.IsMatch(taxIdentifier, jurisdiction.TaxIdentifierPattern, RegexOptions.None, MatchTimeout);
    }

    public static TaxIdentifier Normalise(CountryCode countryCode, TaxIdentifier taxIdentifier)
    {
        var prefix = TaxJurisdictions.RegistrationPrefix(countryCode);

        return ((string)taxIdentifier).StartsWith(prefix, StringComparison.Ordinal)
            ? taxIdentifier
            : new TaxIdentifier(prefix + taxIdentifier);
    }
}
