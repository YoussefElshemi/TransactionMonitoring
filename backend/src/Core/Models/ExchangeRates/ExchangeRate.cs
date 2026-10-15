using Core.Enums;
using Core.Models.BaseModels;
using Core.ValueObjects;

namespace Core.Models.ExchangeRates;

public record ExchangeRate : BaseModel
{
    public required ExchangeRateId ExchangeRateId { get; init; }
    public required Currency FromCurrency { get; init; }
    public required Currency ToCurrency { get; init; }

    public required ConversionRate Rate { get; init; }

    public required EffectiveFrom EffectiveFrom { get; init; }
    public required ApplicationUserId? CreatedByUserId { get; init; }
}
