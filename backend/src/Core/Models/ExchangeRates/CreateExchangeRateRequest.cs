using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.ExchangeRates;

public record CreateExchangeRateRequest
{
    public required Currency FromCurrency { get; init; }
    public required Currency ToCurrency { get; init; }
    public required ConversionRate Rate { get; init; }

    public required EffectiveFrom? EffectiveFrom { get; init; }

    public required ApplicationUserId? CreatedByUserId { get; init; }
}
