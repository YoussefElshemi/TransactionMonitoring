using Core.Enums;
using Core.Exceptions;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Mappers;
using Core.Models.Common;
using Core.Models.ExchangeRates;
using Core.ValueObjects;

namespace Core.Services;

public class ExchangeRateService(
    IExchangeRateRepository exchangeRateRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IExchangeRateService
{
    public async Task<ExchangeRate> CreateAsync(CreateExchangeRateRequest createExchangeRateRequest, CancellationToken stoppingToken)
    {
        if (createExchangeRateRequest.FromCurrency == createExchangeRateRequest.ToCurrency)
        {
            throw new InvalidPriceStructureException("a currency does not need a rate against itself.");
        }

        var exchangeRate = createExchangeRateRequest.ToModel(timeProvider);

        await exchangeRateRepository.AddAsync(exchangeRate, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);

        return await exchangeRateRepository.GetAsync(exchangeRate.ExchangeRateId, stoppingToken);
    }

    public async Task<ConversionRate> GetRateAsync(Currency fromCurrency, Currency toCurrency, DateTimeOffset asOf, CancellationToken stoppingToken)
    {
        if (fromCurrency == toCurrency)
        {
            return new ConversionRate(1m);
        }

        var exchangeRate = await exchangeRateRepository.GetInEffectAsync(fromCurrency, toCurrency, asOf, stoppingToken);

        return exchangeRate?.Rate ?? throw new MissingExchangeRateException(fromCurrency, toCurrency, asOf);
    }

    public Task<Exists> ExistsAsync(Currency fromCurrency, Currency toCurrency, EffectiveFrom effectiveFrom, CancellationToken stoppingToken)
    {
        return exchangeRateRepository.ExistsAsync(fromCurrency, toCurrency, effectiveFrom, stoppingToken);
    }

    public Task<PagedResponse<ExchangeRate>> ListAsync(ListExchangeRateRequest listExchangeRateRequest, CancellationToken stoppingToken)
    {
        return exchangeRateRepository.ListAsync(listExchangeRateRequest, stoppingToken);
    }

    public Task<TotalCount> GetCountAsync(ListExchangeRateRequest listExchangeRateRequest, CancellationToken stoppingToken)
    {
        return exchangeRateRepository.GetCountAsync(listExchangeRateRequest, stoppingToken);
    }
}
