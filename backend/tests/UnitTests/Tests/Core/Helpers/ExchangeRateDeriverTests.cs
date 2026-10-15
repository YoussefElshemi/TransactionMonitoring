using Core.Enums;
using Core.Helpers;
using Core.Models.ExchangeRates;
using Core.ValueObjects;
using FluentAssertions;
using UnitTests.TestHelpers;

namespace UnitTests.Tests.Core.Helpers;

public class ExchangeRateDeriverTests : BaseTestClass
{
    private static readonly DateTimeOffset PublishedOn = new(2026, 9, 7, 0, 0, 0, TimeSpan.Zero);

    private static ExchangeRateQuote Quote(params (Currency Currency, decimal Rate)[] rates)
    {
        return new ExchangeRateQuote
        {
            BaseCurrency = Currency.Eur,
            EffectiveFrom = new EffectiveFrom(PublishedOn),
            Rates = rates.ToDictionary(x => x.Currency, x => new ConversionRate(x.Rate))
        };
    }

    private static decimal RateOf(List<CreateExchangeRateRequest> requests, Currency from, Currency to)
    {
        return requests.Single(x => x.FromCurrency == from && x.ToCurrency == to).Rate;
    }

    [Fact]
    public void Derive_GivenAFullQuote_ProducesEveryOrderedPairAndNoSelfPairs()
    {
        // Arrange
        var quote = Quote((Currency.Gbp, 0.86m), (Currency.Usd, 1.08m));

        // Act
        var requests = ExchangeRateDeriver.Derive(quote);

        // Assert
        requests.Should().HaveCount(6);
        requests.Should().OnlyContain(x => x.FromCurrency != x.ToCurrency);
        requests.Select(x => (x.FromCurrency, x.ToCurrency)).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Derive_GivenABaseCurrencyPair_UsesTheQuoteAsPublished()
    {
        // Arrange
        var quote = Quote((Currency.Gbp, 0.86m), (Currency.Usd, 1.08m));

        // Act
        var requests = ExchangeRateDeriver.Derive(quote);

        // Assert
        RateOf(requests, Currency.Eur, Currency.Gbp).Should().Be(0.86m);
        RateOf(requests, Currency.Eur, Currency.Usd).Should().Be(1.08m);
    }

    [Fact]
    public void Derive_GivenAPairAgainstTheBase_InvertsTheQuote()
    {
        // Arrange
        var quote = Quote((Currency.Gbp, 0.86m), (Currency.Usd, 1.08m));

        // Act
        var requests = ExchangeRateDeriver.Derive(quote);

        // Assert
        RateOf(requests, Currency.Gbp, Currency.Eur).Should().Be(Math.Round(1m / 0.86m, 6, MidpointRounding.AwayFromZero));
    }

    [Fact]
    public void Derive_GivenTwoNonBaseCurrencies_CrossesTheirQuotes()
    {
        // Arrange
        var quote = Quote((Currency.Gbp, 0.86m), (Currency.Usd, 1.08m));

        // Act
        var requests = ExchangeRateDeriver.Derive(quote);

        // Assert
        RateOf(requests, Currency.Gbp, Currency.Usd).Should().Be(Math.Round(1.08m / 0.86m, 6, MidpointRounding.AwayFromZero));
        RateOf(requests, Currency.Usd, Currency.Gbp).Should().Be(Math.Round(0.86m / 1.08m, 6, MidpointRounding.AwayFromZero));
    }

    [Fact]
    public void Derive_GivenARateThatDoesNotDivideEvenly_RoundsToWhatTheDocumentPrints()
    {
        // Arrange
        var quote = Quote((Currency.Gbp, 0.86m), (Currency.Usd, 1.08m));

        // Act
        var requests = ExchangeRateDeriver.Derive(quote);

        // Assert
        requests.Should().OnlyContain(x => decimal.Round(x.Rate, 6) == x.Rate);
    }

    [Fact]
    public void Derive_GivenAQuoteMissingACurrency_LeavesThatCurrencySPairsOut()
    {
        // Arrange
        var quote = Quote((Currency.Gbp, 0.86m));

        // Act
        var requests = ExchangeRateDeriver.Derive(quote);

        // Assert
        requests.Should().NotContain(x => x.FromCurrency == Currency.Usd || x.ToCurrency == Currency.Usd);
        requests.Should().HaveCount(2);
    }

    [Fact]
    public void Derive_GivenAQuote_CarriesItsPublicationInstantOntoEveryPair()
    {
        // Arrange
        var quote = Quote((Currency.Gbp, 0.86m), (Currency.Usd, 1.08m));

        // Act
        var requests = ExchangeRateDeriver.Derive(quote);

        // Assert
        requests.Should().OnlyContain(x => x.EffectiveFrom!.Value == PublishedOn);

        requests.Should().OnlyContain(x => x.CreatedByUserId == null);
    }
}
