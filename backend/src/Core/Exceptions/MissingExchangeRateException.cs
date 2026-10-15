using System.Net;
using Core.Enums;

namespace Core.Exceptions;

public class MissingExchangeRateException(Currency fromCurrency, Currency toCurrency, DateTimeOffset asOf)
    : DomainException($"No exchange rate from {fromCurrency} to {toCurrency} was in force on {asOf:yyyy-MM-dd}. Record one before invoicing that period.")
{
    public Currency FromCurrency { get; } = fromCurrency;
    public Currency ToCurrency { get; } = toCurrency;
    public DateTimeOffset AsOf { get; } = asOf;

    public override HttpStatusCode GetStatusCode()
    {
        return HttpStatusCode.Conflict;
    }

    public override string GetTitle()
    {
        return "No exchange rate in force";
    }
}
