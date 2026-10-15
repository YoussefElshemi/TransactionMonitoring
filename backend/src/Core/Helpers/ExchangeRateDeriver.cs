using Core.Enums;
using Core.Models.ExchangeRates;
using Core.ValueObjects;

namespace Core.Helpers;

public static class ExchangeRateDeriver
{
    private const int RateDecimals = 6;

    public static List<CreateExchangeRateRequest> Derive(ExchangeRateQuote exchangeRateQuote)
    {
        var currencies = Enum.GetValues<Currency>();
        var requests = new List<CreateExchangeRateRequest>();

        foreach (var fromCurrency in currencies)
        {
            foreach (var toCurrency in currencies)
            {
                if (fromCurrency == toCurrency)
                {
                    continue;
                }

                var request = Derive(exchangeRateQuote, fromCurrency, toCurrency);

                if (request is not null)
                {
                    requests.Add(request);
                }
            }
        }

        return requests;
    }

    private static CreateExchangeRateRequest? Derive(ExchangeRateQuote exchangeRateQuote, Currency fromCurrency, Currency toCurrency)
    {
        var from = Quote(exchangeRateQuote, fromCurrency);
        var to = Quote(exchangeRateQuote, toCurrency);

        if (from is null || to is null)
        {
            return null;
        }

        var rate = Math.Round(to.Value / from.Value, RateDecimals, MidpointRounding.AwayFromZero);

        if (rate <= 0)
        {
            return null;
        }

        return new CreateExchangeRateRequest
        {
            FromCurrency = fromCurrency,
            ToCurrency = toCurrency,
            Rate = new ConversionRate(rate),
            EffectiveFrom = exchangeRateQuote.EffectiveFrom,

            CreatedByUserId = null
        };
    }

    private static decimal? Quote(ExchangeRateQuote exchangeRateQuote, Currency currency)
    {
        if (currency == exchangeRateQuote.BaseCurrency)
        {
            return 1m;
        }

        return exchangeRateQuote.Rates.TryGetValue(currency, out var rate) ? rate : null;
    }
}
