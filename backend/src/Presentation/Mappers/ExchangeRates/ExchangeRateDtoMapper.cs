using Core.Helpers;
using Core.Models.ExchangeRates;
using Core.ValueObjects;
using Presentation.Models.ExchangeRates;

namespace Presentation.Mappers.ExchangeRates;

public static class ExchangeRateDtoMapper
{
    public static CreateExchangeRateRequest ToModel(this CreateExchangeRateRequestDto dto)
    {
        return new CreateExchangeRateRequest
        {
            FromCurrency = dto.Body.FromCurrency,
            ToCurrency = dto.Body.ToCurrency,
            Rate = new ConversionRate(dto.Body.Rate),
            EffectiveFrom = dto.Body.EffectiveFrom.HasValue ? new EffectiveFrom(dto.Body.EffectiveFrom.Value) : null,
            CreatedByUserId = dto.CreatedByUserId.HasValue ? new ApplicationUserId(dto.CreatedByUserId.Value) : null
        };
    }

    public static ListExchangeRateRequest ToModel(this ListExchangeRateRequestDto dto)
    {
        return new ListExchangeRateRequest
        {
            FromCurrency = dto.FromCurrency,
            ToCurrency = dto.ToCurrency,
            PageNumber = new PageNumber(dto.PageNumber),
            PageSize = new PageSize(dto.PageSize),
            FromDate = new DateRange(dto.FromDate),
            ToDate = new DateRange(dto.ToDate)
        };
    }

    public static ExchangeRateResponseDto ToDto(this ExchangeRate exchangeRate)
    {
        return new ExchangeRateResponseDto
        {
            ExchangeRateId = exchangeRate.ExchangeRateId,
            FromCurrency = CurrencyFormatter.Code(exchangeRate.FromCurrency),
            ToCurrency = CurrencyFormatter.Code(exchangeRate.ToCurrency),
            Rate = exchangeRate.Rate,
            EffectiveFrom = exchangeRate.EffectiveFrom,
            CreatedByUserId = exchangeRate.CreatedByUserId.HasValue ? exchangeRate.CreatedByUserId.Value : null,
            CreatedAt = exchangeRate.CreatedAt
        };
    }
}
