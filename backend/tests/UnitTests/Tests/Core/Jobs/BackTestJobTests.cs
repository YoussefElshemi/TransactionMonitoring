using System.Text.Json.Nodes;
using Core.Configs;
using Core.Enums;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Jobs;
using Core.Models.BackTests;
using Core.Models.Parties;
using Core.Models.Rules;
using Core.Models.Usages;
using Core.Models.VelocityObservations;
using Core.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Quartz;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Configs;
using UnitTests.TestHelpers.FakeObjects.Core.Models.BackTests;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Rules;

namespace UnitTests.Tests.Core.Jobs;

public class BackTestJobTests : BaseTestClass
{
    private readonly Mock<IBackTestRepository> _mockBackTestRepository = new();
    private readonly Mock<IBackTestResultRepository> _mockBackTestResultRepository = new();
    private readonly Mock<IRuleVersionRepository> _mockRuleVersionRepository = new();
    private readonly Mock<IRuleEvaluationRepository> _mockRuleEvaluationRepository = new();
    private readonly Mock<IUsageRepository> _mockUsageRepository = new();
    private readonly Mock<ISandboxCreditService> _mockSandboxCreditService = new();
    private readonly Mock<IPartyService> _mockPartyService = new();
    private readonly Mock<IVelocityService> _mockVelocityService = new();
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();
    private readonly Mock<IOptions<AppConfig>> _mockConfig = new();
    private readonly BackTestJob _sut;

    public BackTestJobTests()
    {
        _mockConfig
            .SetupGet(x => x.Value)
            .Returns(FakeAppConfig.CreateValid(Fixture));

        _mockPartyService
            .Setup(x => x.PeekManyAsync(It.IsAny<OrganizationId>(), It.IsAny<IReadOnlyList<JsonObject>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OrganizationId _, IReadOnlyList<JsonObject> payloads, CancellationToken _) =>
                payloads.Select(_ => new List<PartyFacts>()).ToList());

        _mockVelocityService
            .Setup(x => x.PeekManyAsync(It.IsAny<OrganizationId>(), It.IsAny<IReadOnlyList<VelocityPeekSample>>(), It.IsAny<IReadOnlyList<RuleBodyModel>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OrganizationId _, IReadOnlyList<VelocityPeekSample> samples, IReadOnlyList<RuleBodyModel> _, CancellationToken _) =>
                samples.Select(_ => VelocityFacts.Empty).ToList());

        _sut = new BackTestJob(
            _mockBackTestRepository.Object,
            _mockBackTestResultRepository.Object,
            _mockRuleVersionRepository.Object,
            _mockRuleEvaluationRepository.Object,
            _mockUsageRepository.Object,
            _mockPartyService.Object,
            _mockVelocityService.Object,
            _mockUnitOfWork.Object,
            _mockConfig.Object,
            MockTimeProvider,
            TestModeContext,
            _mockSandboxCreditService.Object,
            MockLogger.Object);
    }

    private static IJobExecutionContext CreateContext(BackTestId backTestId, CancellationToken stoppingToken = default)
    {
        var jobKey = new JobKey(((Guid)backTestId).ToString(), "BackTests");

        var mockJobDetail = new Mock<IJobDetail>();
        mockJobDetail.SetupGet(x => x.Key).Returns(jobKey);

        var mockContext = new Mock<IJobExecutionContext>();
        mockContext.SetupGet(x => x.JobDetail).Returns(mockJobDetail.Object);
        mockContext.SetupGet(x => x.CancellationToken).Returns(stoppingToken);

        return mockContext.Object;
    }

    [Fact]
    public async Task Execute_GivenPendingBackTest_ProcessesAllChunksAndMarksCompleted()
    {
        // Arrange
        var backTest = FakeBackTest.CreateValid(Fixture) with
        {
            SampleSize = new SampleSize(100)
        };

        var ruleVersion = FakeRuleVersion.CreateValid(Fixture);
        var evaluations = new List<BackTestSample>
        {
            FakeBackTestSample.CreateValid(Fixture),
            FakeBackTestSample.CreateValid(Fixture)
        };

        _mockBackTestRepository
            .Setup(x => x.GetAsync(It.IsAny<BackTestId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(backTest);

        _mockRuleVersionRepository
            .Setup(x => x.GetAsync(It.IsAny<RuleVersionId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ruleVersion);

        _mockRuleEvaluationRepository
            .Setup(x => x.CountByRuleAsync(It.IsAny<OrganizationId>(), It.IsAny<RuleId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TotalCount(evaluations.Count));

        _mockRuleEvaluationRepository
            .SetupSequence(x => x.ListChunkForBackTestAsync(
                It.IsAny<OrganizationId>(), It.IsAny<RuleId>(), It.IsAny<BackTestId>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(evaluations)
            .ReturnsAsync(new List<BackTestSample>());

        var context = CreateContext(backTest.BackTestId);

        // Act
        await _sut.Execute(context);

        // Assert
        _mockUsageRepository.Verify(x => x.AddAsync(It.IsAny<Usage>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockBackTestRepository.Verify(x => x.UpdateAsync(
            It.Is<UpdateBackTestRequest>(r =>
                r.Status == BackTestStatus.Completed &&
                r.ProcessedCount == evaluations.Count &&
                r.TotalEvaluations == evaluations.Count &&
                r.CompletedAt != null),
            It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task Execute_GivenSampleSize_ProcessesOnlyTheSampledPortion()
    {
        // Arrange
        var backTest = FakeBackTest.CreateValid(Fixture) with
        {
            SampleSize = new SampleSize(50)
        };
        var ruleVersion = FakeRuleVersion.CreateValid(Fixture);

        var sample = Enumerable.Range(0, 5).Select(_ => FakeBackTestSample.CreateValid(Fixture)).ToList();

        _mockBackTestRepository
            .Setup(x => x.GetAsync(It.IsAny<BackTestId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(backTest);

        _mockRuleVersionRepository
            .Setup(x => x.GetAsync(It.IsAny<RuleVersionId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ruleVersion);

        _mockRuleEvaluationRepository
            .Setup(x => x.CountByRuleAsync(It.IsAny<OrganizationId>(), It.IsAny<RuleId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TotalCount(10));

        _mockRuleEvaluationRepository
            .Setup(x => x.ListChunkForBackTestAsync(
                It.IsAny<OrganizationId>(), It.IsAny<RuleId>(), It.IsAny<BackTestId>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sample);

        var context = CreateContext(backTest.BackTestId);

        // Act
        await _sut.Execute(context);

        // Assert
        _mockRuleEvaluationRepository.Verify(x => x.ListChunkForBackTestAsync(
            It.IsAny<OrganizationId>(), It.IsAny<RuleId>(), It.IsAny<BackTestId>(), 5, It.IsAny<CancellationToken>()), Times.Once);
        _mockBackTestRepository.Verify(x => x.UpdateAsync(
            It.Is<UpdateBackTestRequest>(r =>
                r.Status == BackTestStatus.Completed &&
                r.TotalEvaluations == 5 &&
                r.ProcessedCount == 5),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Execute_GivenExceptionMidRun_RecordsRunningProgressAndMarksFailed()
    {
        // Arrange
        var backTest = FakeBackTest.CreateValid(Fixture) with
        {
            Status = BackTestStatus.Pending,
            SampleSize = new SampleSize(100)
        };
        var ruleVersion = FakeRuleVersion.CreateValid(Fixture);
        var evaluations = new List<BackTestSample>
        {
            FakeBackTestSample.CreateValid(Fixture),
            FakeBackTestSample.CreateValid(Fixture)
        };

        _mockBackTestRepository
            .Setup(x => x.GetAsync(It.IsAny<BackTestId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(backTest);

        _mockRuleVersionRepository
            .Setup(x => x.GetAsync(It.IsAny<RuleVersionId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ruleVersion);

        _mockRuleEvaluationRepository
            .Setup(x => x.CountByRuleAsync(It.IsAny<OrganizationId>(), It.IsAny<RuleId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TotalCount(evaluations.Count * 2));

        _mockRuleEvaluationRepository
            .SetupSequence(x => x.ListChunkForBackTestAsync(
                It.IsAny<OrganizationId>(), It.IsAny<RuleId>(), It.IsAny<BackTestId>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(evaluations)
            .ThrowsAsync(new InvalidOperationException());

        var context = CreateContext(backTest.BackTestId);

        // Act
        await _sut.Execute(context);

        // Assert
        _mockBackTestRepository.Verify(x => x.UpdateAsync(
            It.Is<UpdateBackTestRequest>(r =>
                r.Status == BackTestStatus.Failed &&
                r.ProcessedCount == evaluations.Count),
            It.Is<CancellationToken>(ct => ct == CancellationToken.None)), Times.Once);
    }

    [Fact]
    public async Task Execute_GivenOperationCancelled_RethrowsAndDoesNotMarkFailed()
    {
        // Arrange
        var backTest = FakeBackTest.CreateValid(Fixture) with { Status = BackTestStatus.Pending };
        var ruleVersion = FakeRuleVersion.CreateValid(Fixture);

        _mockBackTestRepository
            .Setup(x => x.GetAsync(It.IsAny<BackTestId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(backTest);

        _mockRuleVersionRepository
            .Setup(x => x.GetAsync(It.IsAny<RuleVersionId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ruleVersion);

        _mockRuleEvaluationRepository
            .Setup(x => x.CountByRuleAsync(It.IsAny<OrganizationId>(), It.IsAny<RuleId>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var context = CreateContext(backTest.BackTestId);

        // Act
        var act = async () => await _sut.Execute(context);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
        _mockBackTestRepository.Verify(x => x.UpdateAsync(
            It.Is<UpdateBackTestRequest>(r => r.Status == BackTestStatus.Failed),
            It.IsAny<CancellationToken>()), Times.Never);
    }
}
