using Core.Enums;
using Core.Models.Common;

namespace Core.Models.ExchangeRates;

public record ListExchangeRateRequest : PagedRequest
{
    public required Currency? FromCurrency { get; init; }
    public required Currency? ToCurrency { get; init; }
}
