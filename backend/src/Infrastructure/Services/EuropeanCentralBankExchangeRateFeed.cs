using System.Diagnostics;
using System.Globalization;
using System.Xml.Linq;
using Core.Configs;
using Core.Enums;
using Core.Interfaces.Services;
using Core.Models.ExchangeRates;
using Core.ValueObjects;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

public class EuropeanCentralBankExchangeRateFeed(
    IHttpClientFactory httpClientFactory,
    IOptions<AppConfig> appConfig) : IExchangeRateFeed
{
    private static readonly XNamespace Namespace = "http://www.ecb.int/vocabulary/2002-08-01/eurofxref";

    public async Task<ExchangeRateQuote?> GetLatestAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var httpClient = httpClientFactory.CreateClient();

            var response = await httpClient.GetAsync(
                new Uri(appConfig.Value.ExchangeRateFeedConfig.Url), stoppingToken);

            if (!response.IsSuccessStatusCode)
            {
                Activity.Current?.AddEvent(new ActivityEvent("Exchange Rate Feed Unavailable"));

                return null;
            }

            return Parse(await response.Content.ReadAsStringAsync(stoppingToken));
        }
        catch (Exception exception)
        {
            Activity.Current?.AddException(exception);

            return null;
        }
    }

    private static ExchangeRateQuote? Parse(string xml)
    {
        var dayCube = XDocument.Parse(xml)
            .Descendants(Namespace + "Cube")
            .FirstOrDefault(x => x.Attribute("time") is not null);

        if (dayCube is null)
        {
            return null;
        }

        if (!DateTimeOffset.TryParse(
                dayCube.Attribute("time")!.Value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var effectiveFrom))
        {
            return null;
        }

        var rates = new Dictionary<Currency, ConversionRate>();

        foreach (var currencyCube in dayCube.Elements(Namespace + "Cube"))
        {
            var code = currencyCube.Attribute("currency")?.Value;
            var rate = currencyCube.Attribute("rate")?.Value;

            if (code is null || rate is null)
            {
                continue;
            }

            if (!Enum.TryParse<Currency>(code, ignoreCase: true, out var currency) || !Enum.IsDefined(currency))
            {
                continue;
            }

            if (!decimal.TryParse(rate, NumberStyles.Number, CultureInfo.InvariantCulture, out var value) || value <= 0)
            {
                continue;
            }

            rates[currency] = new ConversionRate(value);
        }

        if (rates.Count == 0)
        {
            return null;
        }

        return new ExchangeRateQuote
        {
            BaseCurrency = Currency.Eur,
            EffectiveFrom = new EffectiveFrom(effectiveFrom),
            Rates = rates
        };
    }
}
