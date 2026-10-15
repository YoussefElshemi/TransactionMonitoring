using Core.Exceptions;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Models.Discounts;
using Core.Services;
using Core.ValueObjects;
using FluentAssertions;
using Moq;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Discounts;

namespace UnitTests.Tests.Core.Services;

public class ContractDiscountServiceTests : BaseTestClass
{
    private readonly Mock<IContractDiscountRepository> _mockContractDiscountRepository = new();
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();
    private readonly ContractDiscountService _sut;

    public ContractDiscountServiceTests()
    {
        _mockContractDiscountRepository
            .Setup(x => x.GetAsync(It.IsAny<ContractDiscountId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakeContractDiscount.CreateValid(Fixture));

        _sut = new ContractDiscountService(_mockContractDiscountRepository.Object, _mockUnitOfWork.Object, MockTimeProvider);
    }

    [Fact]
    public async Task CreateAsync_GivenAValidDiscount_WritesItAndSaves()
    {
        // Arrange
        var request = FakeCreateContractDiscountRequest.CreateValid(Fixture);

        // Act
        await _sut.CreateAsync(request, CancellationToken.None);

        // Assert
        _mockContractDiscountRepository.Verify(x => x.AddAsync(It.IsAny<ContractDiscount>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_GivenAWindowThatEndsBeforeItStarts_Throws()
    {
        // Arrange
        var now = MockTimeProvider.GetUtcNow();
        var request = FakeCreateContractDiscountRequest.CreateValid(Fixture) with
        {
            EffectiveFrom = new EffectiveFrom(now),
            EffectiveTo = new EffectiveTo(now.AddDays(-1))
        };

        // Act
        var act = async () => await _sut.CreateAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidPriceStructureException>();

        _mockContractDiscountRepository.Verify(x => x.AddAsync(It.IsAny<ContractDiscount>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_GivenADiscountAlreadyInForce_EndsItRatherThanRemovingIt()
    {
        // Arrange
        var discount = FakeContractDiscount.CreateValid(Fixture) with
        {
            EffectiveFrom = new EffectiveFrom(MockTimeProvider.GetUtcNow().AddDays(-1))
        };

        _mockContractDiscountRepository
            .Setup(x => x.GetAsync(It.IsAny<ContractDiscountId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(discount);

        // Act
        await _sut.DeleteAsync(discount.ContractDiscountId, CancellationToken.None);

        // Assert
        _mockContractDiscountRepository.Verify(
            x => x.EndAsync(discount.ContractDiscountId, It.IsAny<EffectiveTo>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockContractDiscountRepository.Verify(
            x => x.DeleteAsync(It.IsAny<ContractDiscountId>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_GivenADiscountThatHasAlreadyExpired_DoesNotExtendItsEnd()
    {
        // Arrange
        var now = MockTimeProvider.GetUtcNow();
        var expiredAt = now.AddMonths(-3);

        var discount = FakeContractDiscount.CreateValid(Fixture) with
        {
            EffectiveFrom = new EffectiveFrom(now.AddMonths(-6)),
            EffectiveTo = new EffectiveTo(expiredAt)
        };

        _mockContractDiscountRepository
            .Setup(x => x.GetAsync(It.IsAny<ContractDiscountId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(discount);

        // Act
        await _sut.DeleteAsync(discount.ContractDiscountId, CancellationToken.None);

        // Assert
        _mockContractDiscountRepository.Verify(
            x => x.EndAsync(discount.ContractDiscountId, new EffectiveTo(expiredAt), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_GivenAnOpenEndedDiscountInForce_EndsItNow()
    {
        // Arrange
        var now = MockTimeProvider.GetUtcNow();

        var discount = FakeContractDiscount.CreateValid(Fixture) with
        {
            EffectiveFrom = new EffectiveFrom(now.AddMonths(-1)),
            EffectiveTo = null
        };

        _mockContractDiscountRepository
            .Setup(x => x.GetAsync(It.IsAny<ContractDiscountId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(discount);

        // Act
        await _sut.DeleteAsync(discount.ContractDiscountId, CancellationToken.None);

        // Assert
        _mockContractDiscountRepository.Verify(
            x => x.EndAsync(discount.ContractDiscountId, new EffectiveTo(now), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_GivenADiscountStillRunning_EndsItNowRatherThanAtItsAgreedEnd()
    {
        // Arrange
        var now = MockTimeProvider.GetUtcNow();

        var discount = FakeContractDiscount.CreateValid(Fixture) with
        {
            EffectiveFrom = new EffectiveFrom(now.AddMonths(-1)),
            EffectiveTo = new EffectiveTo(now.AddMonths(6))
        };

        _mockContractDiscountRepository
            .Setup(x => x.GetAsync(It.IsAny<ContractDiscountId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(discount);

        // Act
        await _sut.DeleteAsync(discount.ContractDiscountId, CancellationToken.None);

        // Assert
        _mockContractDiscountRepository.Verify(
            x => x.EndAsync(discount.ContractDiscountId, new EffectiveTo(now), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_GivenADiscountThatHasNotStarted_DeletesIt()
    {
        // Arrange
        var discount = FakeContractDiscount.CreateValid(Fixture) with
        {
            EffectiveFrom = new EffectiveFrom(MockTimeProvider.GetUtcNow().AddDays(1))
        };

        _mockContractDiscountRepository
            .Setup(x => x.GetAsync(It.IsAny<ContractDiscountId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(discount);

        // Act
        await _sut.DeleteAsync(discount.ContractDiscountId, CancellationToken.None);

        // Assert
        _mockContractDiscountRepository.Verify(
            x => x.DeleteAsync(discount.ContractDiscountId, It.IsAny<CancellationToken>()), Times.Once);
        _mockContractDiscountRepository.Verify(
            x => x.EndAsync(It.IsAny<ContractDiscountId>(), It.IsAny<EffectiveTo>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
