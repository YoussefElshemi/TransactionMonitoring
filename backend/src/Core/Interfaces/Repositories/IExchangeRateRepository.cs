using Core.Enums;
using Core.Models.ExchangeRates;
using Core.ValueObjects;

namespace Core.Interfaces.Repositories;

public interface IExchangeRateRepository : IPagedRepository<ListExchangeRateRequest, ExchangeRate>
{
    Task AddAsync(ExchangeRate exchangeRate, CancellationToken stoppingToken);
    Task<ExchangeRate> GetAsync(ExchangeRateId exchangeRateId, CancellationToken stoppingToken);

    Task<ExchangeRate?> GetInEffectAsync(Currency fromCurrency, Currency toCurrency, DateTimeOffset asOf, CancellationToken stoppingToken);

    // The feed republishes the last working day's rates on weekends and holidays.
    Task<Exists> ExistsAsync(Currency fromCurrency, Currency toCurrency, EffectiveFrom effectiveFrom, CancellationToken stoppingToken);
}
