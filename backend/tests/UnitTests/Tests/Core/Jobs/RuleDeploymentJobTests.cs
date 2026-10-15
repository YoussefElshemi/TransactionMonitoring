using Core.Constants;
using Core.Enums;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Jobs;
using Core.Models.RuleDeployments;
using Core.Models.Rules;
using Core.ValueObjects;
using Moq;
using Quartz;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.RuleDeployments;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Rules;

namespace UnitTests.Tests.Core.Jobs;

public class RuleDeploymentJobTests : BaseTestClass
{
    private readonly Mock<IRuleDeploymentRepository> _mockRuleDeploymentRepository = new();
    private readonly Mock<IRuleVersionRepository> _mockRuleVersionRepository = new();
    private readonly Mock<IRuleRepository> _mockRuleRepository = new();
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();
    private readonly Mock<ISchedulerFactory> _mockSchedulerFactory = new();
    private readonly Mock<IScheduler> _mockScheduler = new();
    private readonly RuleDeploymentJob _sut;

    public RuleDeploymentJobTests()
    {
        _mockSchedulerFactory
            .Setup(x => x.GetScheduler(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_mockScheduler.Object);

        _sut = new RuleDeploymentJob(
            _mockRuleDeploymentRepository.Object,
            _mockRuleVersionRepository.Object,
            _mockRuleRepository.Object,
            _mockUnitOfWork.Object,
            _mockSchedulerFactory.Object,
            MockTimeProvider,
            TestModeContext,
            MockLogger.Object);
    }

    private static IJobExecutionContext CreateContext(RuleDeploymentId ruleDeploymentId)
    {
        var jobKey = new JobKey(((Guid)ruleDeploymentId).ToString(), QuartzConstants.RuleDeploymentGroup);

        var mockJobDetail = new Mock<IJobDetail>();
        mockJobDetail.SetupGet(x => x.Key).Returns(jobKey);

        var mockContext = new Mock<IJobExecutionContext>();
        mockContext.SetupGet(x => x.JobDetail).Returns(mockJobDetail.Object);
        mockContext.SetupGet(x => x.CancellationToken).Returns(CancellationToken.None);

        return mockContext.Object;
    }

    [Fact]
    public async Task Execute_GivenInProgressBelowHundred_AdvancesPercentage()
    {
        // Arrange
        var deployment = FakeRuleDeployment.CreateValid(Fixture) with
        {
            Status = RuleDeploymentStatus.InProgress,
            CurrentPercentage = new RolloutPercentage(20),
            StepPercentage = new StepPercentage(20)
        };

        _mockRuleDeploymentRepository
            .Setup(x => x.GetAsync(It.IsAny<RuleDeploymentId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(deployment);

        // Act
        await _sut.Execute(CreateContext(deployment.RuleDeploymentId));

        // Assert
        _mockRuleDeploymentRepository.Verify(x => x.UpdateAsync(
            It.Is<UpdateRuleDeploymentRequest>(r =>
                r.Status == RuleDeploymentStatus.InProgress &&
                r.CurrentPercentage == new RolloutPercentage(40) &&
                r.CompletedAt == null),
            It.IsAny<CancellationToken>()), Times.Once);
        _mockRuleRepository.Verify(x => x.DeployVersionAsync(It.IsAny<RuleId>(), It.IsAny<RuleVersion>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Execute_GivenReachesHundred_FinalizesAndDeploys()
    {
        // Arrange
        var deployment = FakeRuleDeployment.CreateValid(Fixture) with
        {
            Status = RuleDeploymentStatus.InProgress,
            CurrentPercentage = new RolloutPercentage(80),
            StepPercentage = new StepPercentage(20)
        };
        var targetVersion = FakeRuleVersion.CreateValid(Fixture);

        _mockRuleDeploymentRepository
            .Setup(x => x.GetAsync(It.IsAny<RuleDeploymentId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(deployment);

        _mockRuleVersionRepository
            .Setup(x => x.GetAsync(It.IsAny<RuleVersionId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetVersion);

        // Act
        await _sut.Execute(CreateContext(deployment.RuleDeploymentId));

        // Assert
        _mockRuleRepository.Verify(x => x.DeployVersionAsync(
            deployment.RuleId,
            It.Is<RuleVersion>(v => v.RuleVersionId == targetVersion.RuleVersionId),
            It.IsAny<CancellationToken>()), Times.Once);
        _mockRuleDeploymentRepository.Verify(x => x.UpdateAsync(
            It.Is<UpdateRuleDeploymentRequest>(r =>
                r.Status == RuleDeploymentStatus.Completed &&
                r.CurrentPercentage == new RolloutPercentage(100) &&
                r.CompletedAt != null),
            It.IsAny<CancellationToken>()), Times.Once);
        _mockScheduler.Verify(x => x.DeleteJob(It.IsAny<JobKey>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Execute_WhenProcessingThrows_MarksFailedAndUnschedules()
    {
        // Arrange
        var deployment = FakeRuleDeployment.CreateValid(Fixture) with
        {
            Status = RuleDeploymentStatus.InProgress,
            CurrentPercentage = new RolloutPercentage(80),
            StepPercentage = new StepPercentage(20)
        };

        _mockRuleDeploymentRepository
            .Setup(x => x.GetAsync(It.IsAny<RuleDeploymentId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(deployment);

        _mockRuleVersionRepository
            .Setup(x => x.GetAsync(It.IsAny<RuleVersionId>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        // Act
        await _sut.Execute(CreateContext(deployment.RuleDeploymentId));

        // Assert
        _mockRuleDeploymentRepository.Verify(x => x.UpdateAsync(
            It.Is<UpdateRuleDeploymentRequest>(r =>
                r.Status == RuleDeploymentStatus.Failed &&
                r.CompletedAt != null),
            It.IsAny<CancellationToken>()), Times.Once);
        _mockScheduler.Verify(x => x.DeleteJob(It.IsAny<JobKey>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Execute_GivenNotInProgress_UnschedulesAndDoesNothing()
    {
        // Arrange
        var deployment = FakeRuleDeployment.CreateValid(Fixture) with
        {
            Status = RuleDeploymentStatus.Cancelled
        };

        _mockRuleDeploymentRepository
            .Setup(x => x.GetAsync(It.IsAny<RuleDeploymentId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(deployment);

        // Act
        await _sut.Execute(CreateContext(deployment.RuleDeploymentId));

        // Assert
        _mockRuleDeploymentRepository.Verify(x => x.UpdateAsync(It.IsAny<UpdateRuleDeploymentRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockRuleRepository.Verify(x => x.DeployVersionAsync(It.IsAny<RuleId>(), It.IsAny<RuleVersion>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockScheduler.Verify(x => x.DeleteJob(It.IsAny<JobKey>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
