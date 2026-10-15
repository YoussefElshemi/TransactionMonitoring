using Core.Enums;
using Core.Models.ExchangeRates;
using Core.ValueObjects;

namespace Core.Interfaces.Services;

public interface IExchangeRateService : IPagedService<ListExchangeRateRequest, ExchangeRate>
{
    Task<ExchangeRate> CreateAsync(CreateExchangeRateRequest createExchangeRateRequest, CancellationToken stoppingToken);

    Task<ConversionRate> GetRateAsync(Currency fromCurrency, Currency toCurrency, DateTimeOffset asOf, CancellationToken stoppingToken);

    Task<Exists> ExistsAsync(Currency fromCurrency, Currency toCurrency, EffectiveFrom effectiveFrom, CancellationToken stoppingToken);
}
