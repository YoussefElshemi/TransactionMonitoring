using Core.Enums;
using Presentation.Models.Common;

namespace Presentation.Models.ExchangeRates;

public record ListExchangeRateRequestDto : PagedRequestDto
{
    public Currency? FromCurrency { get; init; }
    public Currency? ToCurrency { get; init; }
}
