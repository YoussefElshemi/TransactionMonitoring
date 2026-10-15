using System.Text.RegularExpressions;

namespace Core.Helpers;

public static partial class CardNumberDetector
{
    private const int MinDigits = 13;
    private const int MaxDigits = 19;

    public static bool IsCardNumber(string? value)
    {
        return TryNormalise(value, out var digits) && IsIssuerPrefix(digits) && PassesLuhn(digits);
    }

    public static bool TryNormalise(string? value, out string digits)
    {
        digits = string.Empty;

        if (string.IsNullOrWhiteSpace(value) || !SeparatedDigits().IsMatch(value))
        {
            return false;
        }

        digits = value.Replace(" ", string.Empty).Replace("-", string.Empty);

        return digits.Length is >= MinDigits and <= MaxDigits;
    }

    public static string LastFour(string digits)
    {
        return digits[^4..];
    }

    private static bool IsIssuerPrefix(string digits)
    {
        var two = int.Parse(digits[..2]);
        var four = int.Parse(digits[..4]);

        return digits[0] switch
        {
            '4' => true,                                    // Visa
            '5' => two is >= 51 and <= 55,                  // Mastercard
            '2' => four is >= 2221 and <= 2720,             // Mastercard, expanded range
            '3' => two is 34 or 37 or 35,                   // American Express, JCB
            '6' => two is 65 or 60 or 62 or 64,             // Discover, UnionPay, Maestro
            _ => false
        };
    }

    private static bool PassesLuhn(string digits)
    {
        var sum = 0;
        var doubling = false;

        for (var position = digits.Length - 1; position >= 0; position--)
        {
            var digit = digits[position] - '0';

            if (doubling)
            {
                digit *= 2;

                if (digit > 9)
                {
                    digit -= 9;
                }
            }

            sum += digit;
            doubling = !doubling;
        }

        return sum % 10 == 0;
    }

    [GeneratedRegex(@"^[0-9][0-9 \-]*[0-9]$")]
    private static partial Regex SeparatedDigits();
}
