using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Models.BackTests;
using Core.Services;
using Core.ValueObjects;
using FluentAssertions;
using Moq;
using Quartz;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.BackTests;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Rules;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.Tests.Core.Services;

public class BackTestServiceTests : BaseTestClass
{
    private readonly Mock<IBackTestRepository> _mockBackTestRepository = new();
    private readonly Mock<IRuleRepository> _mockRuleRepository = new();
    private readonly Mock<IRuleVersionRepository> _mockRuleVersionRepository = new();
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();
    private readonly Mock<ISchedulerFactory> _mockSchedulerFactory = new();
    private readonly Mock<IScheduler> _mockScheduler = new();
    private readonly BackTestService _sut;

    public BackTestServiceTests()
    {
        _mockSchedulerFactory
            .Setup(x => x.GetScheduler(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_mockScheduler.Object);

        _mockRuleRepository
            .Setup(x => x.GetAsync(It.IsAny<RuleId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakeRule.CreateValid(Fixture));

        _mockRuleVersionRepository
            .Setup(x => x.GetByVersionNumberAsync(It.IsAny<RuleId>(), It.IsAny<VersionNumber>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakeRuleVersion.CreateValid(Fixture));

        _sut = new BackTestService(
            _mockBackTestRepository.Object,
            _mockRuleRepository.Object,
            _mockRuleVersionRepository.Object,
            _mockUnitOfWork.Object,
            _mockSchedulerFactory.Object,
            MockTimeProvider,
            TestModeContext);
    }

    [Fact]
    public async Task CreateAsync_GivenCreateBackTestRequest_CreatesBackTestAndSchedulesJob()
    {
        // Arrange
        var createBackTestRequest = FakeCreateBackTestRequest.CreateValid(Fixture);
        var backTest = FakeBackTest.CreateValid(Fixture);
        var targetVersion = FakeRuleVersion.CreateValid(Fixture);

        BackTest? addedBackTest = null;

        _mockRuleVersionRepository
            .Setup(x => x.GetByVersionNumberAsync(It.IsAny<RuleId>(), It.IsAny<VersionNumber>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetVersion);

        _mockBackTestRepository
            .Setup(x => x.AddAsync(It.IsAny<BackTest>(), It.IsAny<CancellationToken>()))
            .Callback<BackTest, CancellationToken>((b, _) => addedBackTest = b);

        _mockBackTestRepository
            .Setup(x => x.GetAsync(It.IsAny<BackTestId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(backTest);

        _mockScheduler
            .Setup(x => x.ScheduleJob(It.IsAny<IJobDetail>(), It.IsAny<ITrigger>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(DateTimeOffset.UtcNow);

        // Act
        var result = await _sut.CreateAsync(createBackTestRequest, CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(backTest);
        addedBackTest.Should().NotBeNull();
        addedBackTest!.RuleVersionId.Should().Be(targetVersion.RuleVersionId);
        _mockBackTestRepository.Verify(x => x.AddAsync(It.IsAny<BackTest>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockScheduler.Verify(x => x.ScheduleJob(It.IsAny<IJobDetail>(), It.IsAny<ITrigger>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockBackTestRepository.Verify(x => x.GetAsync(It.IsAny<BackTestId>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExistsActiveAsync_GivenOrganizationIdAndRuleId_ReturnsExists()
    {
        // Arrange
        var organizationId = FakeOrganizationId.CreateValid(Fixture);
        var ruleId = FakeRuleId.CreateValid(Fixture);
        var expected = new Exists(true);

        _mockBackTestRepository
            .Setup(x => x.ExistsActiveAsync(It.IsAny<OrganizationId>(), It.IsAny<RuleId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _sut.ExistsActiveAsync(organizationId, ruleId, CancellationToken.None);

        // Assert
        result.Should().Be(expected);
        _mockBackTestRepository.Verify(x => x.ExistsActiveAsync(organizationId, ruleId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
