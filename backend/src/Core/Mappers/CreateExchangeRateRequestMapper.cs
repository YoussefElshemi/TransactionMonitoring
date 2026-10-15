using Core.Models.ExchangeRates;
using Core.ValueObjects;

namespace Core.Mappers;

public static class CreateExchangeRateRequestMapper
{
    public static ExchangeRate ToModel(this CreateExchangeRateRequest request, TimeProvider timeProvider)
    {
        var now = timeProvider.GetUtcNow();

        return new ExchangeRate
        {
            ExchangeRateId = new ExchangeRateId(Guid.NewGuid()),
            FromCurrency = request.FromCurrency,
            ToCurrency = request.ToCurrency,
            Rate = request.Rate,
            EffectiveFrom = request.EffectiveFrom ?? new EffectiveFrom(now),
            CreatedByUserId = request.CreatedByUserId,
            CreatedAt = new CreatedAt(now),
            UpdatedAt = new UpdatedAt(now)
        };
    }
}
