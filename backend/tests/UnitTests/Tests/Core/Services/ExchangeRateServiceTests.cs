using Core.Enums;
using Core.Exceptions;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Models.ExchangeRates;
using Core.Services;
using Core.ValueObjects;
using FluentAssertions;
using Moq;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.Tests.Core.Services;

public class ExchangeRateServiceTests : BaseTestClass
{
    private readonly Mock<IExchangeRateRepository> _mockExchangeRateRepository = new();
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();
    private readonly ExchangeRateService _sut;

    public ExchangeRateServiceTests()
    {
        _sut = new ExchangeRateService(
            _mockExchangeRateRepository.Object,
            _mockUnitOfWork.Object,
            MockTimeProvider);
    }

    private ExchangeRate Rate(decimal rate)
    {
        return new ExchangeRate
        {
            ExchangeRateId = FakeExchangeRateId.CreateValid(Fixture),
            FromCurrency = Currency.Gbp,
            ToCurrency = Currency.Eur,
            Rate = new ConversionRate(rate),
            EffectiveFrom = new EffectiveFrom(MockTimeProvider.GetUtcNow()),
            CreatedByUserId = null,
            CreatedAt = FakeCreatedAt.CreateValid(Fixture),
            UpdatedAt = FakeUpdatedAt.CreateValid(Fixture)
        };
    }

    [Fact]
    public async Task GetRateAsync_GivenOneCurrency_IsOneWithoutReadingAnything()
    {
        // Arrange
        // Act
        var rate = await _sut.GetRateAsync(Currency.Gbp, Currency.Gbp, MockTimeProvider.GetUtcNow(), CancellationToken.None);

        // Assert
        rate.Should().Be(new ConversionRate(1m));

        _mockExchangeRateRepository.Verify(
            x => x.GetInEffectAsync(It.IsAny<Currency>(), It.IsAny<Currency>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetRateAsync_GivenARecordedRate_ReturnsIt()
    {
        // Arrange
        _mockExchangeRateRepository
            .Setup(x => x.GetInEffectAsync(It.IsAny<Currency>(), It.IsAny<Currency>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Rate(1.17m));

        // Act
        var rate = await _sut.GetRateAsync(Currency.Gbp, Currency.Eur, MockTimeProvider.GetUtcNow(), CancellationToken.None);

        // Assert
        rate.Should().Be(new ConversionRate(1.17m));
    }

    [Fact]
    public async Task GetRateAsync_GivenNoRecordedRate_Throws()
    {
        // Arrange
        _mockExchangeRateRepository
            .Setup(x => x.GetInEffectAsync(It.IsAny<Currency>(), It.IsAny<Currency>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExchangeRate?)null);

        // Act
        var act = async () => await _sut.GetRateAsync(Currency.Gbp, Currency.Eur, MockTimeProvider.GetUtcNow(), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<MissingExchangeRateException>();
    }

    [Fact]
    public async Task CreateAsync_GivenTheSameCurrencyBothWays_Throws()
    {
        // Arrange
        var request = new CreateExchangeRateRequest
        {
            FromCurrency = Currency.Gbp,
            ToCurrency = Currency.Gbp,
            Rate = new ConversionRate(1m),
            EffectiveFrom = null,
            CreatedByUserId = null
        };

        // Act
        var act = async () => await _sut.CreateAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidPriceStructureException>();

        _mockExchangeRateRepository.Verify(x => x.AddAsync(It.IsAny<ExchangeRate>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
