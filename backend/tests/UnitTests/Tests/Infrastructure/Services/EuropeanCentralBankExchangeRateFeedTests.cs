using System.Net;
using Core.Configs;
using Core.Enums;
using Core.ValueObjects;
using FluentAssertions;
using Infrastructure.Services;
using Microsoft.Extensions.Options;
using Moq;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Configs;

namespace UnitTests.Tests.Infrastructure.Services;

public class EuropeanCentralBankExchangeRateFeedTests : BaseTestClass
{
    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory = new();
    private readonly Mock<IOptions<AppConfig>> _mockConfig = new();
    private readonly EuropeanCentralBankExchangeRateFeed _sut;

    private const string Publication =
        """
        <?xml version="1.0" encoding="UTF-8"?>
        <gesmes:Envelope xmlns:gesmes="http://www.gesmes.org/xml/2002-08-01" xmlns="http://www.ecb.int/vocabulary/2002-08-01/eurofxref">
            <gesmes:subject>Reference rates</gesmes:subject>
            <Cube>
                <Cube time="2026-09-07">
                    <Cube currency="USD" rate="1.0800"/>
                    <Cube currency="JPY" rate="159.42"/>
                    <Cube currency="GBP" rate="0.8600"/>
                </Cube>
            </Cube>
        </gesmes:Envelope>
        """;

    public EuropeanCentralBankExchangeRateFeedTests()
    {
        _mockConfig
            .SetupGet(x => x.Value)
            .Returns(FakeAppConfig.CreateValid(Fixture));

        _sut = new EuropeanCentralBankExchangeRateFeed(_mockHttpClientFactory.Object, _mockConfig.Object);
    }

    private void GivenFeedResponds(HttpStatusCode statusCode, string? content = null)
    {
        _mockHttpClientFactory
            .Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(SetUpHttpClient(statusCode, content));
    }

    [Fact]
    public async Task GetLatestAsync_GivenAPublication_ReadsItsDateAndTheCurrenciesThePlatformPricesIn()
    {
        // Arrange
        GivenFeedResponds(HttpStatusCode.OK, Publication);

        // Act
        var quote = await _sut.GetLatestAsync(CancellationToken.None);

        // Assert
        quote.Should().NotBeNull();
        quote!.BaseCurrency.Should().Be(Currency.Eur);
        ((DateTimeOffset)quote.EffectiveFrom).Should().Be(new DateTimeOffset(2026, 9, 7, 0, 0, 0, TimeSpan.Zero));

        quote.Rates[Currency.Gbp].Should().Be(new ConversionRate(0.86m));
        quote.Rates[Currency.Usd].Should().Be(new ConversionRate(1.08m));

        quote.Rates.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetLatestAsync_GivenAnUnavailableFeed_ReturnsNull()
    {
        // Arrange
        GivenFeedResponds(HttpStatusCode.ServiceUnavailable);

        // Act
        var quote = await _sut.GetLatestAsync(CancellationToken.None);

        // Assert
        quote.Should().BeNull();
    }

    [Fact]
    public async Task GetLatestAsync_GivenSomethingThatIsNotTheDocument_ReturnsNull()
    {
        // Arrange
        GivenFeedResponds(HttpStatusCode.OK, "<html><body>Service temporarily unavailable</body></html>");

        // Act
        var quote = await _sut.GetLatestAsync(CancellationToken.None);

        // Assert
        quote.Should().BeNull();
    }

    [Fact]
    public async Task GetLatestAsync_GivenAPublicationOfNoCurrenciesThePlatformPricesIn_ReturnsNull()
    {
        // Arrange
        GivenFeedResponds(HttpStatusCode.OK, Publication.Replace("USD", "JPY").Replace("GBP", "CHF"));

        // Act
        var quote = await _sut.GetLatestAsync(CancellationToken.None);

        // Assert
        quote.Should().BeNull();
    }

    [Fact]
    public async Task GetLatestAsync_GivenTheFeedThrows_ReturnsNull()
    {
        // Arrange
        _mockHttpClientFactory
            .Setup(x => x.CreateClient(It.IsAny<string>()))
            .Throws(new HttpRequestException("No route to host"));

        // Act
        var quote = await _sut.GetLatestAsync(CancellationToken.None);

        // Assert
        quote.Should().BeNull();
    }
}
