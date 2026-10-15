using Core.Enums;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Models.RuleDeployments;
using Core.Models.Rules;
using Core.Services;
using Core.ValueObjects;
using FluentAssertions;
using FluentValidation;
using Moq;
using Quartz;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.RuleDeployments;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Rules;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.Tests.Core.Services;

public class RuleDeploymentServiceTests : BaseTestClass
{
    private readonly Mock<IRuleRepository> _mockRuleRepository = new();
    private readonly Mock<IRuleVersionRepository> _mockRuleVersionRepository = new();
    private readonly Mock<IRuleDeploymentRepository> _mockRuleDeploymentRepository = new();
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();
    private readonly Mock<IApprovalRepository> _mockApprovalRepository = new();
    private readonly Mock<IApprovalPolicyRepository> _mockApprovalPolicyRepository = new();
    private readonly Mock<IWebhookConfigurationService> _mockWebhookConfigurationService = new();
    private readonly Mock<IOutboxMessageService> _mockOutboxMessageService = new();
    private readonly Mock<ISchedulerFactory> _mockSchedulerFactory = new();
    private readonly Mock<IScheduler> _mockScheduler = new();
    private readonly RuleDeploymentService _sut;

    public RuleDeploymentServiceTests()
    {
        _mockSchedulerFactory
            .Setup(x => x.GetScheduler(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_mockScheduler.Object);

        _mockRuleDeploymentRepository
            .Setup(x => x.GetAsync(It.IsAny<RuleDeploymentId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakeRuleDeployment.CreateValid(Fixture));

        _sut = new RuleDeploymentService(
            _mockRuleRepository.Object,
            _mockRuleVersionRepository.Object,
            _mockRuleDeploymentRepository.Object,
            _mockApprovalRepository.Object,
            _mockApprovalPolicyRepository.Object,
            _mockWebhookConfigurationService.Object,
            _mockOutboxMessageService.Object,
            _mockUnitOfWork.Object,
            _mockSchedulerFactory.Object,
            MockTimeProvider,
            TestModeContext);
    }

    [Fact]
    public async Task DeployAsync_AllAtOnce_DeploysImmediatelyAndRecordsCompletedDeployment()
    {
        // Arrange
        var rule = FakeRule.CreateValid(Fixture);
        var targetVersion = FakeRuleVersion.CreateValid(Fixture) with { RuleId = rule.RuleId, VersionNumber = new VersionNumber(2) };
        var deployRuleRequest = new DeployRuleRequest
        {
            RuleId = rule.RuleId,
            VersionNumber = new VersionNumber(2),
            Strategy = DeploymentStrategy.AllAtOnce,
            StepPercentage = null,
            StepIntervalSeconds = null,
            DeployedByUserId = FakeApplicationUserId.CreateValid(Fixture)
        };

        RuleDeployment? addedDeployment = null;

        _mockRuleRepository
            .Setup(x => x.GetAsync(It.IsAny<RuleId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rule);

        _mockRuleVersionRepository
            .Setup(x => x.GetByVersionNumberAsync(It.IsAny<RuleId>(), It.IsAny<VersionNumber>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetVersion);

        _mockRuleDeploymentRepository
            .Setup(x => x.AddAsync(It.IsAny<RuleDeployment>(), It.IsAny<CancellationToken>()))
            .Callback<RuleDeployment, CancellationToken>((d, _) => addedDeployment = d);

        // Act
        await _sut.DeployAsync(deployRuleRequest, CancellationToken.None);

        // Assert
        _mockRuleRepository.Verify(x => x.DeployVersionAsync(
            rule.RuleId,
            It.Is<RuleVersion>(v => v.RuleVersionId == targetVersion.RuleVersionId),
            It.IsAny<CancellationToken>()), Times.Once);
        addedDeployment.Should().NotBeNull();
        addedDeployment!.Status.Should().Be(RuleDeploymentStatus.Completed);
        addedDeployment.Strategy.Should().Be(DeploymentStrategy.AllAtOnce);
        addedDeployment.CurrentPercentage.Should().Be(new RolloutPercentage(100));
        _mockScheduler.Verify(x => x.ScheduleJob(It.IsAny<IJobDetail>(), It.IsAny<ITrigger>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeployAsync_Gradual_SchedulesJobAndDoesNotDeployImmediately()
    {
        // Arrange
        var rule = FakeRule.CreateValid(Fixture);
        var targetVersion = FakeRuleVersion.CreateValid(Fixture) with { RuleId = rule.RuleId, VersionNumber = new VersionNumber(2) };
        var deployRuleRequest = new DeployRuleRequest
        {
            RuleId = rule.RuleId,
            VersionNumber = new VersionNumber(2),
            Strategy = DeploymentStrategy.Gradual,
            StepPercentage = new StepPercentage(20),
            StepIntervalSeconds = new StepIntervalSeconds(60),
            DeployedByUserId = FakeApplicationUserId.CreateValid(Fixture)
        };

        RuleDeployment? addedDeployment = null;

        _mockRuleRepository
            .Setup(x => x.GetAsync(It.IsAny<RuleId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rule);

        _mockRuleVersionRepository
            .Setup(x => x.GetByVersionNumberAsync(It.IsAny<RuleId>(), It.IsAny<VersionNumber>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetVersion);

        _mockRuleDeploymentRepository
            .Setup(x => x.AddAsync(It.IsAny<RuleDeployment>(), It.IsAny<CancellationToken>()))
            .Callback<RuleDeployment, CancellationToken>((d, _) => addedDeployment = d);

        _mockRuleDeploymentRepository
            .Setup(x => x.GetActiveByRuleIdAsync(It.IsAny<RuleId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => addedDeployment);

        // Act
        await _sut.DeployAsync(deployRuleRequest, CancellationToken.None);

        // Assert
        _mockRuleRepository.Verify(x => x.DeployVersionAsync(It.IsAny<RuleId>(), It.IsAny<RuleVersion>(), It.IsAny<CancellationToken>()), Times.Never);
        addedDeployment.Should().NotBeNull();
        addedDeployment!.Status.Should().Be(RuleDeploymentStatus.InProgress);
        addedDeployment.Strategy.Should().Be(DeploymentStrategy.Gradual);
        addedDeployment.CurrentPercentage.Should().Be(new RolloutPercentage(20));
        addedDeployment.StepPercentage.Should().Be(new StepPercentage(20));
        addedDeployment.PreviousRuleVersionId.Should().Be(rule.CurrentRuleVersionId);
        _mockScheduler.Verify(x => x.ScheduleJob(It.IsAny<IJobDetail>(), It.IsAny<ITrigger>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeployAsync_Gradual_WhenAnotherDeploymentWonTheRace_ThrowsConflictAndDoesNotSchedule()
    {
        // Arrange
        var rule = FakeRule.CreateValid(Fixture);
        var targetVersion = FakeRuleVersion.CreateValid(Fixture) with { RuleId = rule.RuleId, VersionNumber = new VersionNumber(2) };
        var deployRuleRequest = new DeployRuleRequest
        {
            RuleId = rule.RuleId,
            VersionNumber = new VersionNumber(2),
            Strategy = DeploymentStrategy.Gradual,
            StepPercentage = new StepPercentage(20),
            StepIntervalSeconds = new StepIntervalSeconds(60),
            DeployedByUserId = FakeApplicationUserId.CreateValid(Fixture)
        };

        _mockRuleRepository
            .Setup(x => x.GetAsync(It.IsAny<RuleId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rule);

        _mockRuleVersionRepository
            .Setup(x => x.GetByVersionNumberAsync(It.IsAny<RuleId>(), It.IsAny<VersionNumber>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetVersion);

        _mockRuleDeploymentRepository
            .Setup(x => x.GetActiveByRuleIdAsync(It.IsAny<RuleId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakeRuleDeployment.CreateValid(Fixture) with { Status = RuleDeploymentStatus.InProgress });

        // Act
        var act = async () => await _sut.DeployAsync(deployRuleRequest, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
        _mockScheduler.Verify(x => x.ScheduleJob(It.IsAny<IJobDetail>(), It.IsAny<ITrigger>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CancelDeploymentAsync_WhenDeploymentAlreadyTerminal_DoesNothing()
    {
        // Arrange
        var deployment = FakeRuleDeployment.CreateValid(Fixture) with
        {
            Status = RuleDeploymentStatus.Completed
        };

        _mockRuleDeploymentRepository
            .Setup(x => x.GetAsync(It.IsAny<RuleDeploymentId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(deployment);

        // Act
        await _sut.CancelDeploymentAsync(deployment.RuleDeploymentId, CancellationToken.None);

        // Assert
        _mockRuleDeploymentRepository.Verify(x => x.UpdateAsync(It.IsAny<UpdateRuleDeploymentRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockScheduler.Verify(x => x.DeleteJob(It.IsAny<JobKey>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CancelDeploymentAsync_MarksCancelledAndUnschedulesJob()
    {
        // Arrange
        var deployment = FakeRuleDeployment.CreateValid(Fixture) with
        {
            Status = RuleDeploymentStatus.InProgress,
            CurrentPercentage = new RolloutPercentage(40)
        };

        _mockRuleDeploymentRepository
            .Setup(x => x.GetAsync(It.IsAny<RuleDeploymentId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(deployment);

        // Act
        await _sut.CancelDeploymentAsync(deployment.RuleDeploymentId, CancellationToken.None);

        // Assert
        _mockRuleDeploymentRepository.Verify(x => x.UpdateAsync(
            It.Is<UpdateRuleDeploymentRequest>(r =>
                r.Status == RuleDeploymentStatus.Cancelled &&
                r.CompletedAt != null),
            It.IsAny<CancellationToken>()), Times.Once);
        _mockScheduler.Verify(x => x.DeleteJob(It.IsAny<JobKey>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
