using System.Security.Cryptography;
using System.Text;
using Core.ValueObjects;

namespace Core.Helpers;

public static class CardTokeniser
{
    public const string Prefix = "tok_";

    private const int DigestCharacters = 32;

    public static string Tokenise(DataKey dataKey, string digits)
    {
        var digest = HMACSHA256.HashData(dataKey, Encoding.UTF8.GetBytes(digits));

        return Prefix + Convert.ToHexString(digest)[..DigestCharacters].ToLowerInvariant() + "_" + CardNumberDetector.LastFour(digits);
    }

    public static bool IsToken(string? value)
    {
        return value is not null && value.StartsWith(Prefix, StringComparison.Ordinal);
    }

    public static string? LastFourOf(string token)
    {
        var separator = token.LastIndexOf('_');

        return separator < 0 || separator == token.Length - 1 ? null : token[(separator + 1)..];
    }
}
