using Core.Enums;
using Core.Models.ExchangeRates;
using Core.ValueObjects;
using Infrastructure.Entities;

namespace Infrastructure.Mappers;

public static class ExchangeRateMapper
{
    public static ExchangeRate ToModel(this ExchangeRateEntity exchangeRateEntity)
    {
        return new ExchangeRate
        {
            ExchangeRateId = new ExchangeRateId(exchangeRateEntity.ExchangeRateId),
            FromCurrency = (Currency)exchangeRateEntity.FromCurrencyId,
            ToCurrency = (Currency)exchangeRateEntity.ToCurrencyId,
            Rate = new ConversionRate(exchangeRateEntity.Rate),
            EffectiveFrom = new EffectiveFrom(exchangeRateEntity.EffectiveFrom),
            CreatedByUserId = exchangeRateEntity.CreatedByUserId.HasValue ? new ApplicationUserId(exchangeRateEntity.CreatedByUserId.Value) : null,
            CreatedAt = new CreatedAt(exchangeRateEntity.CreatedAt),
            UpdatedAt = new UpdatedAt(exchangeRateEntity.UpdatedAt)
        };
    }

    public static ExchangeRateEntity ToEntity(this ExchangeRate exchangeRate)
    {
        return new ExchangeRateEntity
        {
            ExchangeRateId = exchangeRate.ExchangeRateId,
            FromCurrencyId = (int)exchangeRate.FromCurrency,
            ToCurrencyId = (int)exchangeRate.ToCurrency,
            Rate = exchangeRate.Rate,
            EffectiveFrom = exchangeRate.EffectiveFrom,
            CreatedByUserId = exchangeRate.CreatedByUserId.HasValue ? exchangeRate.CreatedByUserId.Value : null,
            CreatedAt = exchangeRate.CreatedAt,
            UpdatedAt = exchangeRate.UpdatedAt
        };
    }
}
