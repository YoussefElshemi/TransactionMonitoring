using Core.Configs;
using Core.Enums;
using Core.Interfaces.Services;
using Core.Jobs;
using Core.Models.ExchangeRates;
using Core.ValueObjects;
using Microsoft.Extensions.Options;
using Moq;
using Quartz;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Configs;

namespace UnitTests.Tests.Core.Jobs;

public class RecordExchangeRateJobTests : BaseTestClass
{
    private readonly Mock<IExchangeRateFeed> _mockExchangeRateFeed = new();
    private readonly Mock<IExchangeRateService> _mockExchangeRateService = new();
    private readonly Mock<IOptions<AppConfig>> _mockConfig = new();

    private static readonly DateTimeOffset PublishedOn = new(2026, 9, 7, 0, 0, 0, TimeSpan.Zero);

    public RecordExchangeRateJobTests()
    {
        GivenFeedEnabled(true);

        _mockExchangeRateService
            .Setup(x => x.ExistsAsync(It.IsAny<Currency>(), It.IsAny<Currency>(), It.IsAny<EffectiveFrom>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(false));
    }

    private void GivenFeedEnabled(bool enabled)
    {
        _mockConfig
            .SetupGet(x => x.Value)
            .Returns(FakeAppConfig.CreateValid(Fixture) with
            {
                ExchangeRateFeedConfig = new ExchangeRateFeedConfig { Enabled = enabled }
            });
    }

    private void GivenPublication(ExchangeRateQuote? exchangeRateQuote)
    {
        _mockExchangeRateFeed
            .Setup(x => x.GetLatestAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(exchangeRateQuote);
    }

    private static ExchangeRateQuote Quote()
    {
        return new ExchangeRateQuote
        {
            BaseCurrency = Currency.Eur,
            EffectiveFrom = new EffectiveFrom(PublishedOn),
            Rates = new Dictionary<Currency, ConversionRate>
            {
                [Currency.Gbp] = new(0.86m),
                [Currency.Usd] = new(1.08m)
            }
        };
    }

    private RecordExchangeRateJob CreateSut()
    {
        return new RecordExchangeRateJob(
            _mockExchangeRateFeed.Object,
            _mockExchangeRateService.Object,
            _mockConfig.Object);
    }

    [Fact]
    public async Task Execute_GivenAPublication_AppendsEveryPairItCovers()
    {
        // Arrange
        GivenPublication(Quote());

        // Act
        await CreateSut().Execute(new Mock<IJobExecutionContext>().Object);

        // Assert
        _mockExchangeRateService.Verify(
            x => x.CreateAsync(It.IsAny<CreateExchangeRateRequest>(), It.IsAny<CancellationToken>()),
            Times.Exactly(6));

        _mockExchangeRateService.Verify(
            x => x.CreateAsync(
                It.Is<CreateExchangeRateRequest>(request =>
                    request.FromCurrency == Currency.Gbp &&
                    request.ToCurrency == Currency.Eur &&
                    request.EffectiveFrom!.Value == PublishedOn),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Execute_GivenAPublicationAlreadyRecorded_WritesNothing()
    {
        // Arrange
        GivenPublication(Quote());

        _mockExchangeRateService
            .Setup(x => x.ExistsAsync(It.IsAny<Currency>(), It.IsAny<Currency>(), It.IsAny<EffectiveFrom>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        // Act
        await CreateSut().Execute(new Mock<IJobExecutionContext>().Object);

        // Assert
        _mockExchangeRateService.Verify(
            x => x.CreateAsync(It.IsAny<CreateExchangeRateRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Execute_GivenTheFeedSaidNothing_WritesNothingAndDoesNotThrow()
    {
        // Arrange
        GivenPublication(null);

        // Act
        await CreateSut().Execute(new Mock<IJobExecutionContext>().Object);

        // Assert
        _mockExchangeRateService.Verify(
            x => x.CreateAsync(It.IsAny<CreateExchangeRateRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Execute_GivenTheFeedIsDisabled_DoesNotReachForIt()
    {
        // Arrange
        GivenFeedEnabled(false);
        GivenPublication(Quote());

        // Act
        await CreateSut().Execute(new Mock<IJobExecutionContext>().Object);

        // Assert
        _mockExchangeRateFeed.Verify(x => x.GetLatestAsync(It.IsAny<CancellationToken>()), Times.Never);

        _mockExchangeRateService.Verify(
            x => x.CreateAsync(It.IsAny<CreateExchangeRateRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
