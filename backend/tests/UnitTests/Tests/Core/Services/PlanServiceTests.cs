using Core.Exceptions;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Models.Plans;
using Core.Services;
using Core.ValueObjects;
using FluentAssertions;
using Moq;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Plans;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.Tests.Core.Services;

public class PlanServiceTests : BaseTestClass
{
    private readonly Mock<IPlanRepository> _mockPlanRepository = new();
    private readonly Mock<IPlanVersionRepository> _mockPlanVersionRepository = new();
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();
    private readonly PlanService _sut;

    public PlanServiceTests()
    {
        _sut = new PlanService(
            _mockPlanRepository.Object,
            _mockPlanVersionRepository.Object,
            _mockUnitOfWork.Object,
            MockTimeProvider);
    }

    [Fact]
    public async Task CreateAsync_GivenRequest_PublishesVersionOneAlongsideThePlan()
    {
        // Arrange
        var request = FakeCreatePlanRequest.CreateValid(Fixture);

        Plan? addedPlan = null;
        PlanVersion? addedVersion = null;

        _mockPlanRepository
            .Setup(x => x.AddAsync(It.IsAny<Plan>(), It.IsAny<CancellationToken>()))
            .Callback<Plan, CancellationToken>((x, _) => addedPlan = x);

        _mockPlanVersionRepository
            .Setup(x => x.AddAsync(It.IsAny<PlanVersion>(), It.IsAny<CancellationToken>()))
            .Callback<PlanVersion, CancellationToken>((x, _) => addedVersion = x);

        _mockPlanRepository
            .Setup(x => x.GetAsync(It.IsAny<PlanId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => addedPlan!);

        // Act
        await _sut.CreateAsync(request, CancellationToken.None);

        // Assert
        addedPlan.Should().NotBeNull();
        addedVersion.Should().NotBeNull();

        addedPlan!.Name.Should().Be(request.Name);
        addedPlan.CurrentVersionNumber.Should().Be(new VersionNumber(1));

        addedVersion!.PlanId.Should().Be(addedPlan.PlanId);
        addedVersion.VersionNumber.Should().Be(new VersionNumber(1));
        addedVersion.CostPerRequest.Should().Be(request.CostPerRequest);
    }

    [Fact]
    public async Task PublishVersionAsync_GivenPlan_AddsTheNextVersionAndMovesThePointer()
    {
        // Arrange
        var request = FakePublishPlanVersionRequest.CreateValid(Fixture);

        _mockPlanVersionRepository
            .Setup(x => x.GetNextVersionNumberAsync(It.IsAny<PlanId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VersionNumber(3));

        // Act
        var planVersion = await _sut.PublishVersionAsync(request, CancellationToken.None);

        // Assert
        planVersion.VersionNumber.Should().Be(new VersionNumber(3));
        planVersion.CostPerRequest.Should().Be(request.CostPerRequest);

        _mockPlanVersionRepository.Verify(
            x => x.AddAsync(It.Is<PlanVersion>(v => v.VersionNumber == new VersionNumber(3)), It.IsAny<CancellationToken>()),
            Times.Once);

        _mockPlanRepository.Verify(
            x => x.SetCurrentVersionAsync(request.PlanId, new VersionNumber(3), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_GivenPlanWithNoSubscriptions_DeletesIt()
    {
        // Arrange
        var plan = FakePlan.CreateValid(Fixture);

        _mockPlanRepository
            .Setup(x => x.GetAsync(It.IsAny<PlanId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);

        _mockPlanRepository
            .Setup(x => x.HasSubscriptionsAsync(It.IsAny<PlanId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(false));

        // Act
        await _sut.DeleteAsync(plan.PlanId, CancellationToken.None);

        // Assert
        _mockPlanRepository.Verify(x => x.DeleteAsync(plan.PlanId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_GivenPlanWithSubscriptions_Throws()
    {
        // Arrange
        var plan = FakePlan.CreateValid(Fixture);

        _mockPlanRepository
            .Setup(x => x.GetAsync(It.IsAny<PlanId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);

        _mockPlanRepository
            .Setup(x => x.HasSubscriptionsAsync(It.IsAny<PlanId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        // Act
        var act = async () => await _sut.DeleteAsync(plan.PlanId, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<PlanInUseException>();

        _mockPlanRepository.Verify(x => x.DeleteAsync(It.IsAny<PlanId>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_GivenRequest_UpdatesSavesAndReturnsTheReloadedPlan()
    {
        // Arrange
        var plan = FakePlan.CreateValid(Fixture);
        var request = new UpdatePlanRequest
        {
            PlanId = plan.PlanId,
            Name = FakePlanName.CreateValid(Fixture),
            Description = default,
            IsEnabled = new IsEnabled(false)
        };

        _mockPlanRepository
            .Setup(x => x.GetAsync(It.IsAny<PlanId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);

        // Act
        var updated = await _sut.UpdateAsync(request, CancellationToken.None);

        // Assert
        updated.Should().Be(plan);

        _mockPlanRepository.Verify(x => x.UpdateAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockPlanVersionRepository.Verify(
            x => x.AddAsync(It.IsAny<PlanVersion>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_GivenRequest_SavesThePlanAndItsFirstVersionTogether()
    {
        // Arrange
        var request = FakeCreatePlanRequest.CreateValid(Fixture);

        _mockPlanRepository
            .Setup(x => x.GetAsync(It.IsAny<PlanId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakePlan.CreateValid(Fixture));

        // Act
        await _sut.CreateAsync(request, CancellationToken.None);

        // Assert
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
