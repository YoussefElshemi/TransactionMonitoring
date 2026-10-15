using Core.Models.ExchangeRates;

namespace Core.Interfaces.Services;

public interface IExchangeRateFeed
{
    Task<ExchangeRateQuote?> GetLatestAsync(CancellationToken stoppingToken);
}
