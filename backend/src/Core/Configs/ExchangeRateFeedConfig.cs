namespace Core.Configs;

public record ExchangeRateFeedConfig
{
    public bool Enabled { get; init; } = true;

    public string Url { get; init; } = "https://www.ecb.europa.eu/stats/eurofxref/eurofxref-daily.xml";
}
