using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.ExchangeRates;

public record ExchangeRateQuote
{
    public required Currency BaseCurrency { get; init; }

    public required EffectiveFrom EffectiveFrom { get; init; }

    public required Dictionary<Currency, ConversionRate> Rates { get; init; }
}
