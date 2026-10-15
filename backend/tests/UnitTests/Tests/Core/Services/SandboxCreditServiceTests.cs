using System.Net;
using Core.Configs;
using Core.Enums;
using Core.Exceptions;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Models.Usages;
using Core.Services;
using Core.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Configs;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Organizations;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Usages;

namespace UnitTests.Tests.Core.Services;

public class SandboxCreditServiceTests : BaseTestClass
{
    private const int HouseAllowance = 1000;

    private readonly Mock<IUsageService> _mockUsageService = new();
    private readonly Mock<IOrganizationService> _mockOrganizationService = new();
    private readonly Mock<ISandboxCreditRepository> _mockSandboxCreditRepository = new();
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();
    private readonly SandboxCreditService _sut;

    public SandboxCreditServiceTests()
    {
        var appConfig = FakeAppConfig.CreateValid(Fixture) with
        {
            SandboxConfig = new SandboxConfig { DefaultMonthlyCredits = HouseAllowance }
        };

        var mockConfig = new Mock<IOptions<AppConfig>>();
        mockConfig.SetupGet(x => x.Value).Returns(appConfig);

        _mockUnitOfWork
            .Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task<Usage>>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<Task<Usage>> operation, CancellationToken _) => operation());

        _mockUsageService
            .Setup(x => x.CreateAsync(It.IsAny<CreateUsageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakeUsage.CreateValid(Fixture));

        _sut = new SandboxCreditService(
            _mockUsageService.Object,
            _mockOrganizationService.Object,
            _mockSandboxCreditRepository.Object,
            _mockUnitOfWork.Object,
            mockConfig.Object,
            MockTimeProvider,
            TestModeContext);

        TestModeContext.Current = Mode.Test;

        GivenOrganizationAllowance(null);
        GivenUsedThisMonth(0);
    }

    [Fact]
    public async Task GetAsync_GivenNoNegotiatedAllowance_FallsBackToTheHouseAllowance()
    {
        // Act
        var credit = await _sut.GetAsync(new OrganizationId(Guid.NewGuid()), CancellationToken.None);

        // Assert
        ((int)credit.Allowance).Should().Be(HouseAllowance);
    }

    [Fact]
    public async Task GetAsync_GivenANegotiatedAllowance_UsesIt()
    {
        // Arrange
        GivenOrganizationAllowance(25_000);

        // Act
        var credit = await _sut.GetAsync(new OrganizationId(Guid.NewGuid()), CancellationToken.None);

        // Assert
        ((int)credit.Allowance).Should().Be(25_000);
    }

    [Fact]
    public async Task GetAsync_Always_ReportsWhatIsLeft()
    {
        // Arrange
        GivenUsedThisMonth(400);

        // Act
        var credit = await _sut.GetAsync(new OrganizationId(Guid.NewGuid()), CancellationToken.None);

        // Assert
        ((int)credit.Used).Should().Be(400);
        ((int)credit.Remaining).Should().Be(600);
    }

    [Fact]
    public async Task GetAsync_GivenTheAllowanceWasLoweredBelowWhatIsSpent_ReportsNothingLeft()
    {
        // Arrange
        GivenOrganizationAllowance(100);
        GivenUsedThisMonth(400);

        // Act
        var credit = await _sut.GetAsync(new OrganizationId(Guid.NewGuid()), CancellationToken.None);

        // Assert
        ((int)credit.Remaining).Should().Be(0);
    }

    [Fact]
    public async Task GetAsync_Always_ResetsAtTheStartOfNextMonth()
    {
        // Act
        var credit = await _sut.GetAsync(new OrganizationId(Guid.NewGuid()), CancellationToken.None);

        // Assert
        var now = MockTimeProvider.GetUtcNow();
        var expected = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero).AddMonths(1);

        ((DateTimeOffset)credit.ResetsAt).Should().Be(expected);
    }

    [Fact]
    public async Task EnforceAsync_GivenEnoughLeft_Allows()
    {
        // Arrange
        GivenUsedThisMonth(900);

        // Act
        var act = () => EnforceAsync(100);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task EnforceAsync_GivenTheCostWouldOvershoot_Refuses()
    {
        // Arrange
        GivenUsedThisMonth(950);

        // Act
        var act = () => EnforceAsync(100);

        // Assert
        await act.Should().ThrowAsync<SandboxCreditsExhaustedException>();
    }

    [Fact]
    public async Task EnforceAsync_GivenNothingLeft_Refuses()
    {
        // Arrange
        GivenUsedThisMonth(HouseAllowance);

        // Act
        var act = () => EnforceAsync(1);

        // Assert
        var thrown = await act.Should().ThrowAsync<SandboxCreditsExhaustedException>();

        thrown.Which.GetStatusCode().Should().Be(HttpStatusCode.PaymentRequired);
    }

    [Fact]
    public async Task EnforceAsync_GivenItFits_RecordsWhatItCharged()
    {
        // Act
        await EnforceAsync(7);

        // Assert
        _mockUsageService.Verify(
            x => x.CreateAsync(It.Is<CreateUsageRequest>(y => y.Count == 7), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task EnforceAsync_GivenItDoesNotFit_ChargesNothing()
    {
        // Arrange
        GivenUsedThisMonth(HouseAllowance);

        // Act
        var act = () => EnforceAsync(1);

        // Assert
        await act.Should().ThrowAsync<SandboxCreditsExhaustedException>();

        _mockUsageService.Verify(
            x => x.CreateAsync(It.IsAny<CreateUsageRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EnforceAsync_Always_LocksTheOrganizationBeforeReadingTheBalance()
    {
        // Arrange
        var organizationId = new OrganizationId(Guid.NewGuid());

        var locked = false;
        var lockedBeforeRead = false;

        _mockSandboxCreditRepository
            .Setup(x => x.LockAsync(organizationId, It.IsAny<CancellationToken>()))
            .Callback(() => locked = true)
            .Returns(Task.CompletedTask);

        _mockUsageService
            .Setup(x => x.SumByDateAsync(It.IsAny<SumUsageByDateRequest>(), It.IsAny<CancellationToken>()))
            .Callback(() => lockedBeforeRead = locked)
            .ReturnsAsync(new Sum(0));

        // Act
        await _sut.EnforceAsync(organizationId, BillableResource.RuleEvaluation, new Resource("rule"), new Count(1), CancellationToken.None);

        // Assert
        lockedBeforeRead.Should().BeTrue();
    }

    [Fact]
    public async Task EnforceAsync_Always_TakesTheLockAndTheChargeInOneTransaction()
    {
        // Act
        await EnforceAsync(1);

        // Assert
        _mockUnitOfWork.Verify(
            x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task<Usage>>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EnforceAsync_InLiveMode_NeverRefuses()
    {
        // Arrange
        TestModeContext.Current = Mode.Live;
        GivenUsedThisMonth(HouseAllowance * 10);

        // Act
        var act = () => EnforceAsync(5000);

        // Assert
        await act.Should().NotThrowAsync();

        _mockUsageService.Verify(
            x => x.SumByDateAsync(It.IsAny<SumUsageByDateRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EnforceAsync_InLiveMode_ChargesNothing()
    {
        // Arrange
        TestModeContext.Current = Mode.Live;

        // Act
        await EnforceAsync(5);

        // Assert
        _mockUsageService.Verify(
            x => x.CreateAsync(It.IsAny<CreateUsageRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EnforceAsync_GivenACallThatEvaluatesNothing_StillCostsOne()
    {
        // Arrange
        GivenUsedThisMonth(HouseAllowance);

        // Act
        var act = () => EnforceAsync(0);

        // Assert
        await act.Should().ThrowAsync<SandboxCreditsExhaustedException>();
    }

    [Fact]
    public async Task EnsureAvailableAsync_GivenItFits_ChargesNothing()
    {
        // Act
        await _sut.EnsureAvailableAsync(new OrganizationId(Guid.NewGuid()), new Count(500), CancellationToken.None);

        // Assert
        _mockUsageService.Verify(
            x => x.CreateAsync(It.IsAny<CreateUsageRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EnsureAvailableAsync_GivenItWouldNotFit_Refuses()
    {
        // Arrange
        GivenUsedThisMonth(900);

        // Act
        var act = () => _sut.EnsureAvailableAsync(
            new OrganizationId(Guid.NewGuid()), new Count(500), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<SandboxCreditsExhaustedException>();
    }

    [Fact]
    public async Task GetAsync_CalledFromLiveMode_StillCountsSandboxUsage()
    {
        // Arrange
        TestModeContext.Current = Mode.Live;

        Mode? modeDuringRead = null;

        _mockUsageService
            .Setup(x => x.SumByDateAsync(It.IsAny<SumUsageByDateRequest>(), It.IsAny<CancellationToken>()))
            .Callback(() => modeDuringRead = TestModeContext.Current)
            .ReturnsAsync(new Sum(0));

        // Act
        await _sut.GetAsync(new OrganizationId(Guid.NewGuid()), CancellationToken.None);

        // Assert
        modeDuringRead.Should().Be(Mode.Test);
        TestModeContext.Current.Should().Be(Mode.Live);
    }

    private Task EnforceAsync(int cost)
    {
        return _sut.EnforceAsync(
            new OrganizationId(Guid.NewGuid()), BillableResource.RuleEvaluation, new Resource("rule"), new Count(cost), CancellationToken.None);
    }

    private void GivenOrganizationAllowance(int? allowance)
    {
        _mockOrganizationService
            .Setup(x => x.GetAsync(It.IsAny<OrganizationId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakeOrganization.CreateValid(Fixture) with
            {
                SandboxMonthlyCredits = allowance.HasValue ? new SandboxCredits(allowance.Value) : null
            });
    }

    private void GivenUsedThisMonth(int used)
    {
        _mockUsageService
            .Setup(x => x.SumByDateAsync(It.IsAny<SumUsageByDateRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Sum(used));
    }
}
