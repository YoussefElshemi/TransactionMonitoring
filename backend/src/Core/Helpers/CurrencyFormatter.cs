using System.Globalization;
using Core.Enums;

namespace Core.Helpers;

public static class CurrencyFormatter
{
    public static string Format(Currency currency, decimal amount, int decimals = 2)
    {
        var formatted = Math.Abs(amount).ToString("N" + decimals, CultureInfo.InvariantCulture);
        var sign = amount < 0 ? "-" : string.Empty;

        return sign + Symbol(currency) + formatted;
    }

    public static string Symbol(Currency currency)
    {
        return currency switch
        {
            Currency.Gbp => "£",
            Currency.Eur => "€",
            Currency.Usd => "$",
            _ => currency.ToString().ToUpperInvariant() + " "
        };
    }

    public static string Code(Currency currency)
    {
        return currency.ToString().ToUpperInvariant();
    }
}
