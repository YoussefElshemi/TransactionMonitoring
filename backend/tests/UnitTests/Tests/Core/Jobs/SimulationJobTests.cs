using Core.Configs;
using Core.Enums;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Jobs;
using Core.Models.RateLimits;
using Core.Models.RuleSets;
using Core.Models.Simulations;
using Core.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Quartz;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Configs;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Rules;

namespace UnitTests.Tests.Core.Jobs;

public class SimulationJobTests : BaseTestClass
{
    private const int PaymentCount = 3;

    private readonly Mock<ISimulationRepository> _mockSimulationRepository = new();
    private readonly Mock<ITransactionSchemaRepository> _mockTransactionSchemaRepository = new();
    private readonly Mock<IPartyMappingRepository> _mockPartyMappingRepository = new();
    private readonly Mock<IRuleSetService> _mockRuleSetService = new();
    private readonly Mock<IDistributedRateLimiter> _mockDistributedRateLimiter = new();
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();
    private readonly Mock<ISchedulerFactory> _mockSchedulerFactory = new();
    private readonly Mock<IOptions<AppConfig>> _mockConfig = new();

    private readonly SimulationId _simulationId = new(Guid.NewGuid());
    private readonly OrganizationId _organizationId = new(Guid.NewGuid());

    public SimulationJobTests()
    {
        _mockSchedulerFactory
            .Setup(x => x.GetScheduler(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Mock<IScheduler>().Object);

        _mockRuleSetService
            .Setup(x => x.EvaluateAllAsync(It.IsAny<EvaluateRuleSetsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakeEvaluateAllRulesResponse.CreateValid(Fixture));

        SetUpConfig(FakeRateLimitConfig.CreateValid(permitLimit: 1_000, replenishmentPeriodSeconds: 1) with
        {
            Distributed = new DistributedRateLimitConfig { Enabled = true }
        });
    }

    [Fact]
    public async Task Execute_ForEveryPayment_SpendsTheOrganisationSandboxEvaluationBucket()
    {
        // Arrange
        SetUpSimulation();
        SetUpDecision(RateLimitOutcome.Allowed);

        var expectedKey =
            $"ratelimit:{{org:{(Guid)_organizationId}}}:{Mode.Test}:{RateLimitPool.Evaluation}";

        // Act
        await CreateSut().Execute(CreateContext());

        // Assert
        _mockDistributedRateLimiter.Verify(
            x => x.AcquireAsync(
                It.Is<RateLimitAcquireRequest>(request =>
                    request.Buckets.Count == 1 && (string)request.Buckets[0].Key == expectedKey),
                It.IsAny<CancellationToken>()),
            Times.Exactly(PaymentCount));

        _mockRuleSetService.Verify(
            x => x.EvaluateAllAsync(It.IsAny<EvaluateRuleSetsRequest>(), It.IsAny<CancellationToken>()),
            Times.Exactly(PaymentCount));
    }

    [Fact]
    public async Task Execute_OverBudget_WaitsAndCompletesRatherThanFailingTheRun()
    {
        // Arrange
        SetUpSimulation();

        var rejections = 0;

        _mockDistributedRateLimiter
            .Setup(x => x.AcquireAsync(It.IsAny<RateLimitAcquireRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                rejections++;

                return rejections == 1
                    ? Decision(RateLimitOutcome.Rejected, retryAfter: 1)
                    : Decision(RateLimitOutcome.Allowed);
            });

        // Act
        await CreateSut().Execute(CreateContext());

        // Assert
        _mockRuleSetService.Verify(
            x => x.EvaluateAllAsync(It.IsAny<EvaluateRuleSetsRequest>(), It.IsAny<CancellationToken>()),
            Times.Exactly(PaymentCount));

        _mockSimulationRepository.Verify(
            x => x.UpdateAsync(
                It.Is<UpdateSimulationRequest>(request => request.Status == SimulationStatus.Completed),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _mockSimulationRepository.Verify(
            x => x.UpdateAsync(
                It.Is<UpdateSimulationRequest>(request => request.Status == SimulationStatus.Failed),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Execute_RateLimitingDisabled_RunsWithoutConsultingTheSharedStore()
    {
        // Arrange
        SetUpSimulation();
        SetUpDecision(RateLimitOutcome.Allowed);
        SetUpConfig(FakeRateLimitConfig.CreateValid() with { Enabled = false });

        // Act
        await CreateSut().Execute(CreateContext());

        // Assert
        _mockDistributedRateLimiter.Verify(
            x => x.AcquireAsync(It.IsAny<RateLimitAcquireRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _mockRuleSetService.Verify(
            x => x.EvaluateAllAsync(It.IsAny<EvaluateRuleSetsRequest>(), It.IsAny<CancellationToken>()),
            Times.Exactly(PaymentCount));
    }

    [Fact]
    public async Task Execute_SharedStoreUnavailable_StillPacesTheRunRatherThanRunningFlatOut()
    {
        // Arrange
        SetUpSimulation();
        SetUpDecision(RateLimitOutcome.Unavailable);

        SetUpConfig(FakeRateLimitConfig.CreateValid(permitLimit: 1, replenishmentPeriodSeconds: 1) with
        {
            Distributed = new DistributedRateLimitConfig { Enabled = true }
        });

        var startedAt = DateTimeOffset.UtcNow;

        // Act
        await CreateSut().Execute(CreateContext());

        // Assert
        (DateTimeOffset.UtcNow - startedAt).Should().BeGreaterThan(TimeSpan.FromSeconds(2));

        _mockRuleSetService.Verify(
            x => x.EvaluateAllAsync(It.IsAny<EvaluateRuleSetsRequest>(), It.IsAny<CancellationToken>()),
            Times.Exactly(PaymentCount));
    }

    private void SetUpConfig(RateLimitConfig rateLimitConfig)
    {
        _mockConfig
            .SetupGet(x => x.Value)
            .Returns(FakeAppConfig.CreateValid(Fixture) with { RateLimitConfig = rateLimitConfig });
    }

    private void SetUpSimulation()
    {
        _mockSimulationRepository
            .Setup(x => x.GetAsync(It.IsAny<SimulationId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Simulation
            {
                SimulationId = _simulationId,
                OrganizationId = _organizationId,
                Scenario = SimulationScenario.Baseline,
                Status = SimulationStatus.Pending,
                TotalPayments = new PaymentCount(PaymentCount),
                ProcessedCount = new ProcessedCount(0),
                Seed = new SimulationSeed(7),
                CreatedByUserId = new ApplicationUserId(Guid.NewGuid()),
                CompletedAt = null,
                FailureReason = null,
                Mode = Mode.Test,
                CreatedAt = new CreatedAt(MockTimeProvider.GetUtcNow()),
                UpdatedAt = new UpdatedAt(MockTimeProvider.GetUtcNow())
            });
    }

    private void SetUpDecision(RateLimitOutcome outcome)
    {
        _mockDistributedRateLimiter
            .Setup(x => x.AcquireAsync(It.IsAny<RateLimitAcquireRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Decision(outcome));
    }

    private static RateLimitDecision Decision(RateLimitOutcome outcome, int retryAfter = 0)
    {
        var isUnavailable = outcome == RateLimitOutcome.Unavailable;

        return new RateLimitDecision
        {
            Outcome = outcome,
            PermitLimit = isUnavailable ? null : new PermitLimit(100),
            RemainingPermits = isUnavailable ? null : new RemainingPermits(99),
            RetryAfter = isUnavailable ? null : new RetryAfter(retryAfter)
        };
    }

    private SimulationJob CreateSut()
    {
        return new SimulationJob(
            _mockSimulationRepository.Object,
            _mockTransactionSchemaRepository.Object,
            _mockPartyMappingRepository.Object,
            _mockRuleSetService.Object,
            _mockDistributedRateLimiter.Object,
            _mockUnitOfWork.Object,
            _mockSchedulerFactory.Object,
            TestModeContext,
            _mockConfig.Object,
            MockTimeProvider,
            MockLogger.Object);
    }

    private IJobExecutionContext CreateContext()
    {
        var mockJobDetail = new Mock<IJobDetail>();
        mockJobDetail.SetupGet(x => x.Key).Returns(new JobKey(((Guid)_simulationId).ToString(), "Simulations"));

        var mockContext = new Mock<IJobExecutionContext>();
        mockContext.SetupGet(x => x.JobDetail).Returns(mockJobDetail.Object);
        mockContext.SetupGet(x => x.CancellationToken).Returns(CancellationToken.None);

        return mockContext.Object;
    }
}
