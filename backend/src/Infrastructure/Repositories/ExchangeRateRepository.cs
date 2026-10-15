using Core.Enums;
using Core.Interfaces.Repositories;
using Core.Models.Common;
using Core.Models.ExchangeRates;
using Core.ValueObjects;
using Infrastructure.Entities;
using Infrastructure.Expressions;
using Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class ExchangeRateRepository(
    TransactionMonitoringDbContext context) : PagedRepository<ListExchangeRateRequest, ExchangeRateEntity, ExchangeRate>, IExchangeRateRepository
{
    public async Task AddAsync(ExchangeRate exchangeRate, CancellationToken stoppingToken)
    {
        await context.ExchangeRates.AddAsync(exchangeRate.ToEntity(), stoppingToken);
    }

    public async Task<ExchangeRate> GetAsync(ExchangeRateId exchangeRateId, CancellationToken stoppingToken)
    {
        var exchangeRateEntity = await context.ExchangeRates
            .FirstAsync(x => x.ExchangeRateId == exchangeRateId, stoppingToken);

        return exchangeRateEntity.ToModel();
    }

    public async Task<ExchangeRate?> GetInEffectAsync(Currency fromCurrency, Currency toCurrency, DateTimeOffset asOf, CancellationToken stoppingToken)
    {
        var exchangeRateEntity = await context.ExchangeRates
            .Where(x => x.FromCurrencyId == (int)fromCurrency &&
                        x.ToCurrencyId == (int)toCurrency &&
                        x.EffectiveFrom <= asOf)
            .OrderByDescending(x => x.EffectiveFrom)
            .ThenByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(stoppingToken);

        return exchangeRateEntity?.ToModel();
    }

    public async Task<Exists> ExistsAsync(Currency fromCurrency, Currency toCurrency, EffectiveFrom effectiveFrom, CancellationToken stoppingToken)
    {
        var from = (DateTimeOffset)effectiveFrom;

        var exists = await context.ExchangeRates
            .AnyAsync(x => x.FromCurrencyId == (int)fromCurrency &&
                           x.ToCurrencyId == (int)toCurrency &&
                           x.EffectiveFrom == from, stoppingToken);

        return new Exists(exists);
    }

    protected override Task<List<ExchangeRate>> MapPageAsync(List<ExchangeRateEntity> items, ListExchangeRateRequest request, CancellationToken stoppingToken)
    {
        return Task.FromResult(items.ConvertAll(x => x.ToModel()));
    }

    protected override IOrderByExpression<ExchangeRateEntity> OrderByExpression()
    {
        return new OrderByExpression<ExchangeRateEntity, DateTimeOffset>(x => x.EffectiveFrom);
    }

    protected override IQueryable<ExchangeRateEntity> FilterByPagedRequest(ListExchangeRateRequest listExchangeRateRequest)
    {
        var queryable = context.ExchangeRates.AsQueryable();

        if (listExchangeRateRequest.FromCurrency.HasValue)
        {
            queryable = queryable.Where(x => x.FromCurrencyId == (int)listExchangeRateRequest.FromCurrency.Value);
        }

        if (listExchangeRateRequest.ToCurrency.HasValue)
        {
            queryable = queryable.Where(x => x.ToCurrencyId == (int)listExchangeRateRequest.ToCurrency.Value);
        }

        return queryable;
    }

    protected override IQueryable<ExchangeRateEntity> FilterByDateRange(IQueryable<ExchangeRateEntity> queryable, DatedRequest datedRequest)
    {
        if (datedRequest.FromDate != DateOnly.MinValue)
        {
            queryable = queryable.Where(x => DateOnly.FromDateTime(x.EffectiveFrom.UtcDateTime) >= datedRequest.FromDate);
        }

        if (datedRequest.ToDate != DateOnly.MaxValue)
        {
            queryable = queryable.Where(x => DateOnly.FromDateTime(x.EffectiveFrom.UtcDateTime) <= datedRequest.ToDate);
        }

        return queryable;
    }
}
