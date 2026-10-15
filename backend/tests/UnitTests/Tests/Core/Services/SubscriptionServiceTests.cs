using Core.Exceptions;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Models.Subscriptions;
using Core.Services;
using Core.ValueObjects;
using FluentAssertions;
using Moq;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Subscriptions;

namespace UnitTests.Tests.Core.Services;

public class SubscriptionServiceTests : BaseTestClass
{
    private readonly Mock<ISubscriptionRepository> _mockSubscriptionRepository = new();
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();
    private readonly SubscriptionService _sut;

    public SubscriptionServiceTests()
    {
        _sut = new SubscriptionService(
            _mockSubscriptionRepository.Object,
            _mockUnitOfWork.Object,
            MockTimeProvider);
    }

    private void GivenLatest(Subscription? subscription)
    {
        _mockSubscriptionRepository
            .Setup(x => x.GetLatestAsync(It.IsAny<OrganizationId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);
    }

    private void GivenGetReturnsWhatWasAdded()
    {
        Subscription? added = null;

        _mockSubscriptionRepository
            .Setup(x => x.AddAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()))
            .Callback<Subscription, CancellationToken>((x, _) => added = x);

        _mockSubscriptionRepository
            .Setup(x => x.GetAsync(It.IsAny<SubscriptionId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => added!);
    }

    [Fact]
    public async Task CreateAsync_GivenNoExistingSubscription_OpensOneStartingNow()
    {
        // Arrange
        GivenLatest(null);
        GivenGetReturnsWhatWasAdded();

        var request = FakeCreateSubscriptionRequest.CreateValid(Fixture) with { EffectiveFrom = null };

        // Act
        var subscription = await _sut.CreateAsync(request, CancellationToken.None);

        // Assert
        ((DateTimeOffset)subscription.EffectiveFrom).Should().Be(MockTimeProvider.GetUtcNow());
        subscription.EffectiveTo.Should().BeNull();

        _mockSubscriptionRepository.Verify(
            x => x.CloseAsync(It.IsAny<SubscriptionId>(), It.IsAny<EffectiveTo>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_GivenFutureStart_ClosesTheOpenStretchAtThatInstant()
    {
        // Arrange
        var effectiveFrom = MockTimeProvider.GetUtcNow().AddMonths(1);

        var open = FakeSubscription.CreateValid(Fixture) with
        {
            EffectiveFrom = new EffectiveFrom(MockTimeProvider.GetUtcNow().AddYears(-1)),
            EffectiveTo = null
        };

        GivenLatest(open);
        GivenGetReturnsWhatWasAdded();

        var request = FakeCreateSubscriptionRequest.CreateValid(Fixture) with
        {
            EffectiveFrom = new EffectiveFrom(effectiveFrom)
        };

        // Act
        await _sut.CreateAsync(request, CancellationToken.None);

        // Assert
        _mockSubscriptionRepository.Verify(
            x => x.CloseAsync(
                open.SubscriptionId,
                It.Is<EffectiveTo>(to => to == effectiveFrom),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_GivenTheLatestStretchIsAlreadyClosed_DoesNotCloseItAgain()
    {
        // Arrange
        var closed = FakeSubscription.CreateValid(Fixture) with
        {
            EffectiveFrom = new EffectiveFrom(MockTimeProvider.GetUtcNow().AddYears(-1)),
            EffectiveTo = new EffectiveTo(MockTimeProvider.GetUtcNow().AddMonths(-1))
        };

        GivenLatest(closed);
        GivenGetReturnsWhatWasAdded();

        var request = FakeCreateSubscriptionRequest.CreateValid(Fixture) with
        {
            EffectiveFrom = new EffectiveFrom(MockTimeProvider.GetUtcNow().AddMonths(1))
        };

        // Act
        await _sut.CreateAsync(request, CancellationToken.None);

        // Assert
        _mockSubscriptionRepository.Verify(
            x => x.CloseAsync(It.IsAny<SubscriptionId>(), It.IsAny<EffectiveTo>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _mockSubscriptionRepository.Verify(
            x => x.AddAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_GivenStartEqualToTheLatestStretch_Throws()
    {
        // Arrange
        var effectiveFrom = MockTimeProvider.GetUtcNow().AddMonths(1);

        var latest = FakeSubscription.CreateValid(Fixture) with
        {
            EffectiveFrom = new EffectiveFrom(effectiveFrom),
            EffectiveTo = null
        };

        GivenLatest(latest);

        var request = FakeCreateSubscriptionRequest.CreateValid(Fixture) with
        {
            EffectiveFrom = new EffectiveFrom(effectiveFrom)
        };

        // Act
        var act = async () => await _sut.CreateAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<SubscriptionTimelineException>();

        _mockSubscriptionRepository.Verify(
            x => x.AddAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()), Times.Never);

        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_GivenStartBeforeTheLatestStretch_Throws()
    {
        // Arrange
        var latest = FakeSubscription.CreateValid(Fixture) with
        {
            EffectiveFrom = new EffectiveFrom(MockTimeProvider.GetUtcNow().AddMonths(1)),
            EffectiveTo = null
        };

        GivenLatest(latest);

        var request = FakeCreateSubscriptionRequest.CreateValid(Fixture) with
        {
            EffectiveFrom = new EffectiveFrom(MockTimeProvider.GetUtcNow().AddDays(1))
        };

        // Act
        var act = async () => await _sut.CreateAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<SubscriptionTimelineException>();

        _mockSubscriptionRepository.Verify(
            x => x.AddAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_GivenScheduledChange_CancelsItAndReopensThePredecessor()
    {
        // Arrange
        var scheduled = FakeSubscription.CreateValid(Fixture) with
        {
            EffectiveFrom = new EffectiveFrom(MockTimeProvider.GetUtcNow().AddMonths(1)),
            EffectiveTo = null
        };

        var predecessor = FakeSubscription.CreateValid(Fixture) with
        {
            EffectiveFrom = new EffectiveFrom(MockTimeProvider.GetUtcNow().AddYears(-1)),
            EffectiveTo = new EffectiveTo(MockTimeProvider.GetUtcNow().AddMonths(1))
        };

        _mockSubscriptionRepository
            .Setup(x => x.GetAsync(It.IsAny<SubscriptionId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(scheduled);

        _mockSubscriptionRepository
            .Setup(x => x.GetInEffectAsync(It.IsAny<OrganizationId>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(predecessor);

        // Act
        await _sut.DeleteAsync(scheduled.SubscriptionId, CancellationToken.None);

        // Assert
        _mockSubscriptionRepository.Verify(
            x => x.DeleteAsync(scheduled.SubscriptionId, It.IsAny<CancellationToken>()), Times.Once);

        _mockSubscriptionRepository.Verify(
            x => x.ReopenAsync(predecessor.SubscriptionId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_GivenSubscriptionAlreadyInForce_Throws()
    {
        // Arrange
        var inForce = FakeSubscription.CreateValid(Fixture) with
        {
            EffectiveFrom = new EffectiveFrom(MockTimeProvider.GetUtcNow().AddDays(-1)),
            EffectiveTo = null
        };

        _mockSubscriptionRepository
            .Setup(x => x.GetAsync(It.IsAny<SubscriptionId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(inForce);

        // Act
        var act = async () => await _sut.DeleteAsync(inForce.SubscriptionId, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<SubscriptionTimelineException>();

        _mockSubscriptionRepository.Verify(
            x => x.DeleteAsync(It.IsAny<SubscriptionId>(), It.IsAny<CancellationToken>()), Times.Never);

        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_GivenNoPredecessor_ReopensNothing()
    {
        // Arrange
        var scheduled = FakeSubscription.CreateValid(Fixture) with
        {
            EffectiveFrom = new EffectiveFrom(MockTimeProvider.GetUtcNow().AddMonths(1)),
            EffectiveTo = null
        };

        _mockSubscriptionRepository
            .Setup(x => x.GetAsync(It.IsAny<SubscriptionId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(scheduled);

        _mockSubscriptionRepository
            .Setup(x => x.GetInEffectAsync(It.IsAny<OrganizationId>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);

        // Act
        await _sut.DeleteAsync(scheduled.SubscriptionId, CancellationToken.None);

        // Assert
        _mockSubscriptionRepository.Verify(
            x => x.DeleteAsync(scheduled.SubscriptionId, It.IsAny<CancellationToken>()), Times.Once);

        _mockSubscriptionRepository.Verify(
            x => x.ReopenAsync(It.IsAny<SubscriptionId>(), It.IsAny<CancellationToken>()), Times.Never);

        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_GivenAnOpenEndedPredecessor_LeavesItAlone()
    {
        // Arrange
        var scheduled = FakeSubscription.CreateValid(Fixture) with
        {
            EffectiveFrom = new EffectiveFrom(MockTimeProvider.GetUtcNow().AddMonths(1)),
            EffectiveTo = null
        };

        var predecessor = FakeSubscription.CreateValid(Fixture) with
        {
            EffectiveFrom = new EffectiveFrom(MockTimeProvider.GetUtcNow().AddYears(-1)),
            EffectiveTo = null
        };

        _mockSubscriptionRepository
            .Setup(x => x.GetAsync(It.IsAny<SubscriptionId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(scheduled);

        _mockSubscriptionRepository
            .Setup(x => x.GetInEffectAsync(It.IsAny<OrganizationId>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(predecessor);

        // Act
        await _sut.DeleteAsync(scheduled.SubscriptionId, CancellationToken.None);

        // Assert
        _mockSubscriptionRepository.Verify(
            x => x.ReopenAsync(It.IsAny<SubscriptionId>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_GivenScheduledChange_LooksForThePredecessorAsOfNow()
    {
        // Arrange
        var scheduled = FakeSubscription.CreateValid(Fixture) with
        {
            EffectiveFrom = new EffectiveFrom(MockTimeProvider.GetUtcNow().AddMonths(1)),
            EffectiveTo = null
        };

        _mockSubscriptionRepository
            .Setup(x => x.GetAsync(It.IsAny<SubscriptionId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(scheduled);

        // Act
        await _sut.DeleteAsync(scheduled.SubscriptionId, CancellationToken.None);

        // Assert
        _mockSubscriptionRepository.Verify(
            x => x.GetInEffectAsync(
                scheduled.OrganizationId,
                MockTimeProvider.GetUtcNow(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
