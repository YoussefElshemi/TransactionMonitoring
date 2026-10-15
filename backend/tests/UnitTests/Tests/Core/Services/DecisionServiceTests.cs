using Core.Configs;
using Core.Enums;
using Core.Exceptions;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Models.Cases;
using Core.Models.Decisions;
using Core.Models.OutboxMessages;
using Core.Models.Rules;
using Core.Services;
using Core.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Cases;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Decisions;
using UnitTests.TestHelpers.FakeObjects.Core.Models.WebhookConfigurations;

namespace UnitTests.Tests.Core.Services;

public class DecisionServiceTests : BaseTestClass
{
    private readonly Mock<IAlertRepository> _mockAlertRepository = new();
    private readonly Mock<ICaseRepository> _mockCaseRepository = new();
    private readonly Mock<ICaseNoteRepository> _mockCaseNoteRepository = new();
    private readonly Mock<IDecisionRepository> _mockDecisionRepository = new();
    private readonly Mock<IDecisionRuleLinkRepository> _mockDecisionRuleLinkRepository = new();
    private readonly Mock<IOutboxMessageService> _mockOutboxMessageService = new();
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();
    private readonly Mock<IWebhookConfigurationService> _mockWebhookConfigurationService = new();
    private readonly DecisionService _sut;

    public DecisionServiceTests()
    {
        var appConfig = Options.Create(new AppConfig
        {
            DecisionConfig = new DecisionConfig()
        });

        _sut = new DecisionService(
            _mockDecisionRepository.Object,
            _mockDecisionRuleLinkRepository.Object,
            _mockCaseRepository.Object,
            _mockCaseNoteRepository.Object,
            _mockAlertRepository.Object,
            _mockWebhookConfigurationService.Object,
            _mockOutboxMessageService.Object,
            _mockUnitOfWork.Object,
            appConfig,
            MockTimeProvider,
            TestModeContext);
    }

    [Theory]
    [InlineData(RuleAction.Block, DecisionStatus.Blocked, DecisionReasonCode.RuleBlocked)]
    [InlineData(RuleAction.Review, DecisionStatus.Held, DecisionReasonCode.RuleReviewRequired)]
    [InlineData(RuleAction.Flag, DecisionStatus.Released, DecisionReasonCode.RuleFlagged)]
    [InlineData(RuleAction.Allow, DecisionStatus.Released, DecisionReasonCode.RuleAllowed)]
    public async Task RecordAsync_GivenMatchedOutcome_RecordsDecisionForAction(
        RuleAction action,
        DecisionStatus expectedStatus,
        DecisionReasonCode expectedReasonCode)
    {
        // Arrange
        var recordDecisionRequest = FakeRecordDecisionRequest.CreateValid(Fixture) with
        {
            Outcome = new RuleOutcome
            {
                Action = action,
                Severity = RuleSeverity.High,
                RiskScore = new RiskScore(10)
            },
            AnyMatched = new Matched(true)
        };

        Decision? added = null;

        _mockDecisionRepository
            .Setup(x => x.AddAsync(It.IsAny<Decision>(), It.IsAny<CancellationToken>()))
            .Callback<Decision, CancellationToken>((decision, _) => added = decision);

        _mockDecisionRepository
            .Setup(x => x.GetAsync(It.IsAny<DecisionId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => added!);

        // Act
        var decision = await _sut.RecordAsync(recordDecisionRequest, CancellationToken.None);

        // Assert
        decision.Status.Should().Be(expectedStatus);
        decision.ReasonCode.Should().Be(expectedReasonCode);
        decision.TransactionId.Should().Be(recordDecisionRequest.TransactionId);
        decision.CaseId.Should().Be(recordDecisionRequest.CaseId);
    }

    [Fact]
    public async Task RecordAsync_GivenNothingMatched_ReleasesWithNoRuleMatched()
    {
        // Arrange
        var recordDecisionRequest = FakeRecordDecisionRequest.CreateValid(Fixture) with
        {
            Outcome = new RuleOutcome
            {
                Action = RuleAction.Allow,
                Severity = RuleSeverity.Info,
                RiskScore = new RiskScore(0)
            },
            AnyMatched = new Matched(false),
            CaseId = null
        };

        Decision? added = null;

        _mockDecisionRepository
            .Setup(x => x.AddAsync(It.IsAny<Decision>(), It.IsAny<CancellationToken>()))
            .Callback<Decision, CancellationToken>((decision, _) => added = decision);

        _mockDecisionRepository
            .Setup(x => x.GetAsync(It.IsAny<DecisionId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => added!);

        // Act
        var decision = await _sut.RecordAsync(recordDecisionRequest, CancellationToken.None);

        // Assert
        decision.Status.Should().Be(DecisionStatus.Released);
        decision.ReasonCode.Should().Be(DecisionReasonCode.NoRuleMatched);
        decision.HeldAt.Should().BeNull();
        decision.ResolvedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task RecordAsync_GivenHeldOutcome_StampsHeldAtAndLeavesResolvedAtUnset()
    {
        // Arrange
        var recordDecisionRequest = FakeRecordDecisionRequest.CreateValid(Fixture);

        Decision? added = null;

        _mockDecisionRepository
            .Setup(x => x.AddAsync(It.IsAny<Decision>(), It.IsAny<CancellationToken>()))
            .Callback<Decision, CancellationToken>((decision, _) => added = decision);

        _mockDecisionRepository
            .Setup(x => x.GetAsync(It.IsAny<DecisionId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => added!);

        // Act
        var decision = await _sut.RecordAsync(recordDecisionRequest, CancellationToken.None);

        // Assert
        decision.Status.Should().Be(DecisionStatus.Held);
        decision.HeldAt.Should().NotBeNull();
        decision.ResolvedAt.Should().BeNull();
        decision.ExpiresAt.Should().Be(new ExpiresAt(MockTimeProvider.GetUtcNow() + recordDecisionRequest.HoldPolicy.HoldWindow!.Value.ToTimeSpan()));
        decision.HoldExpiryVerdict.Should().Be(recordDecisionRequest.HoldPolicy.HoldExpiryVerdict);
    }

    [Fact]
    public async Task RecordAsync_GivenAHoldThatNeverExpires_HoldsWithoutADeadline()
    {
        // Arrange
        var recordDecisionRequest = FakeRecordDecisionRequest.CreateValid(Fixture) with
        {
            HoldPolicy = new HoldPolicy
            {
                HoldWindow = null,
                HoldExpiryVerdict = HoldExpiryVerdict.Never
            }
        };

        Decision? added = null;

        _mockDecisionRepository
            .Setup(x => x.AddAsync(It.IsAny<Decision>(), It.IsAny<CancellationToken>()))
            .Callback<Decision, CancellationToken>((decision, _) => added = decision);

        _mockDecisionRepository
            .Setup(x => x.GetAsync(It.IsAny<DecisionId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => added!);

        // Act
        var decision = await _sut.RecordAsync(recordDecisionRequest, CancellationToken.None);

        // Assert
        decision.Status.Should().Be(DecisionStatus.Held);
        decision.HeldAt.Should().NotBeNull();
        decision.ExpiresAt.Should().BeNull();
        decision.HoldExpiryVerdict.Should().Be(HoldExpiryVerdict.Never);
    }

    [Fact]
    public async Task RecordAsync_GivenUnheldOutcome_LeavesTheHoldWindowUnset()
    {
        // Arrange
        var recordDecisionRequest = FakeRecordDecisionRequest.CreateValid(Fixture) with
        {
            Outcome = new RuleOutcome
            {
                Action = RuleAction.Block,
                Severity = RuleSeverity.High,
                RiskScore = new RiskScore(10)
            }
        };

        Decision? added = null;

        _mockDecisionRepository
            .Setup(x => x.AddAsync(It.IsAny<Decision>(), It.IsAny<CancellationToken>()))
            .Callback<Decision, CancellationToken>((decision, _) => added = decision);

        _mockDecisionRepository
            .Setup(x => x.GetAsync(It.IsAny<DecisionId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => added!);

        // Act
        var decision = await _sut.RecordAsync(recordDecisionRequest, CancellationToken.None);

        // Assert
        decision.Status.Should().Be(DecisionStatus.Blocked);
        decision.ExpiresAt.Should().BeNull();
        decision.HoldExpiryVerdict.Should().BeNull();
    }

    [Fact]
    public async Task RecordAsync_GivenTransactionAlreadyDecided_ReturnsExistingDecisionWithoutWriting()
    {
        // Arrange
        var recordDecisionRequest = FakeRecordDecisionRequest.CreateValid(Fixture) with
        {
            CaseId = null
        };
        var existing = FakeDecision.CreateValid(Fixture);

        _mockDecisionRepository
            .Setup(x => x.FindByTransactionAsync(It.IsAny<TransactionId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        // Act
        var decision = await _sut.RecordAsync(recordDecisionRequest, CancellationToken.None);

        // Assert
        decision.Should().Be(existing);

        _mockDecisionRepository.Verify(x => x.AddAsync(It.IsAny<Decision>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RecordAsync_GivenAnotherDecisionWonWhileACaseWasOpened_Throws()
    {
        // Arrange
        var recordDecisionRequest = FakeRecordDecisionRequest.CreateValid(Fixture);
        var existing = FakeDecision.CreateValid(Fixture);

        _mockDecisionRepository
            .Setup(x => x.FindByTransactionAsync(It.IsAny<TransactionId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        // Act
        var act = () => _sut.RecordAsync(recordDecisionRequest, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DecisionAlreadyRecordedException>();

        _mockDecisionRepository.Verify(x => x.AddAsync(It.IsAny<Decision>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RecordAsync_GivenRuleEvaluations_LinksThemToTheDecision()
    {
        // Arrange
        var recordDecisionRequest = FakeRecordDecisionRequest.CreateValid(Fixture);

        List<DecisionRuleEvaluationLink>? linked = null;

        _mockDecisionRuleLinkRepository
            .Setup(x => x.AddRangeAsync(It.IsAny<List<DecisionRuleEvaluationLink>>(), It.IsAny<CancellationToken>()))
            .Callback<List<DecisionRuleEvaluationLink>, CancellationToken>((links, _) => linked = links)
            .Returns(Task.CompletedTask);

        _mockDecisionRepository
            .Setup(x => x.GetAsync(It.IsAny<DecisionId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakeDecision.CreateValid(Fixture));

        // Act
        await _sut.RecordAsync(recordDecisionRequest, CancellationToken.None);

        // Assert
        linked.Should().NotBeNull();
        linked!.ConvertAll(x => x.RuleEvaluationId).Should().Equal(recordDecisionRequest.RuleEvaluationIds);
    }

    [Theory]
    [InlineData(HoldExpiryVerdict.Release, DecisionStatus.Released)]
    [InlineData(HoldExpiryVerdict.Block, DecisionStatus.Blocked)]
    [InlineData(HoldExpiryVerdict.Expire, DecisionStatus.Expired)]
    public async Task ExpireDueAsync_GivenDueHold_AppliesTheStoredVerdictAndClosesTheCase(
        HoldExpiryVerdict holdExpiryVerdict,
        DecisionStatus expectedStatus)
    {
        // Arrange
        var dueDecision = FakeDecision.CreateValid(Fixture) with
        {
            HoldExpiryVerdict = holdExpiryVerdict
        };

        IReadOnlyDictionary<DecisionId, DecisionVerdict>? appliedVerdicts = null;

        _mockDecisionRepository
            .Setup(x => x.ListDueForExpiryAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([dueDecision]);

        _mockDecisionRepository
            .Setup(x => x.ExpireManyAsync(It.IsAny<IReadOnlyDictionary<DecisionId, DecisionVerdict>>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyDictionary<DecisionId, DecisionVerdict>, CancellationToken>((verdicts, _) => appliedVerdicts = verdicts)
            .ReturnsAsync(() =>
            [
                dueDecision with
                {
                    Status = appliedVerdicts![dueDecision.DecisionId].Status,
                    ReasonCode = appliedVerdicts[dueDecision.DecisionId].ReasonCode
                }
            ]);

        _mockDecisionRepository
            .Setup(x => x.ListStatusesByCaseAsync(It.IsAny<IReadOnlyCollection<CaseId>>(), It.IsAny<IReadOnlyCollection<DecisionId>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<CaseId, List<DecisionStatus>> { [dueDecision.CaseId!.Value] = [] });

        _mockCaseRepository
            .Setup(x => x.ExpireManyAsync(It.IsAny<IReadOnlyCollection<CaseId>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([FakeCase.CreateValid(Fixture)]);

        _mockWebhookConfigurationService
            .Setup(x => x.GetAllAsync(It.IsAny<OrganizationId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([FakeWebhookConfiguration.CreateValid(Fixture)]);

        // Act
        var expired = await _sut.ExpireDueAsync(CancellationToken.None);

        // Assert
        expired.Should().Be(new Count(1));

        var appliedVerdict = appliedVerdicts![dueDecision.DecisionId];
        appliedVerdict.Status.Should().Be(expectedStatus);
        appliedVerdict.ReasonCode.Should().Be(DecisionReasonCode.HoldExpired);

        _mockCaseRepository.Verify(
            x => x.ExpireManyAsync(It.Is<IReadOnlyCollection<CaseId>>(caseIds => caseIds.Contains(dueDecision.CaseId!.Value)), It.IsAny<CancellationToken>()),
            Times.Once);
        _mockOutboxMessageService.Verify(x => x.StageManyAsync(It.IsAny<List<CreateOutboxMessageRequest>>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExpireDueAsync_GivenNothingDue_WritesNothing()
    {
        // Arrange
        _mockDecisionRepository
            .Setup(x => x.ListDueForExpiryAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        var expired = await _sut.ExpireDueAsync(CancellationToken.None);

        // Assert
        expired.Should().Be(new Count(0));

        _mockDecisionRepository.Verify(x => x.ExpireManyAsync(It.IsAny<IReadOnlyDictionary<DecisionId, DecisionVerdict>>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockOutboxMessageService.Verify(x => x.StageManyAsync(It.IsAny<List<CreateOutboxMessageRequest>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExpireDueAsync_GivenCaseStillHoldingAnotherPayment_LeavesTheCaseOpen()
    {
        // Arrange
        var dueDecision = FakeDecision.CreateValid(Fixture) with
        {
            HoldExpiryVerdict = HoldExpiryVerdict.Release
        };

        _mockDecisionRepository
            .Setup(x => x.ListDueForExpiryAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([dueDecision]);

        _mockDecisionRepository
            .Setup(x => x.ExpireManyAsync(It.IsAny<IReadOnlyDictionary<DecisionId, DecisionVerdict>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([dueDecision with { Status = DecisionStatus.Released }]);

        _mockDecisionRepository
            .Setup(x => x.ListStatusesByCaseAsync(It.IsAny<IReadOnlyCollection<CaseId>>(), It.IsAny<IReadOnlyCollection<DecisionId>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<CaseId, List<DecisionStatus>> { [dueDecision.CaseId!.Value] = [DecisionStatus.Held] });

        _mockCaseRepository
            .Setup(x => x.ExpireManyAsync(It.IsAny<IReadOnlyCollection<CaseId>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _mockWebhookConfigurationService
            .Setup(x => x.GetAllAsync(It.IsAny<OrganizationId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([FakeWebhookConfiguration.CreateValid(Fixture)]);

        // Act
        await _sut.ExpireDueAsync(CancellationToken.None);

        // Assert
        _mockCaseRepository.Verify(
            x => x.ExpireManyAsync(It.Is<IReadOnlyCollection<CaseId>>(caseIds => caseIds.Count == 0), It.IsAny<CancellationToken>()),
            Times.Once);
        _mockAlertRepository.Verify(x => x.CloseByCaseAsync(It.IsAny<CaseId>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExpireDueAsync_GivenExpiredCase_ClosesItsAlerts()
    {
        // Arrange
        var dueDecision = FakeDecision.CreateValid(Fixture) with
        {
            HoldExpiryVerdict = HoldExpiryVerdict.Expire
        };
        var expiredCase = FakeCase.CreateValid(Fixture);

        _mockDecisionRepository
            .Setup(x => x.ListDueForExpiryAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([dueDecision]);

        _mockDecisionRepository
            .Setup(x => x.ExpireManyAsync(It.IsAny<IReadOnlyDictionary<DecisionId, DecisionVerdict>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([dueDecision with { Status = DecisionStatus.Expired }]);

        _mockDecisionRepository
            .Setup(x => x.ListStatusesByCaseAsync(It.IsAny<IReadOnlyCollection<CaseId>>(), It.IsAny<IReadOnlyCollection<DecisionId>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<CaseId, List<DecisionStatus>> { [dueDecision.CaseId!.Value] = [DecisionStatus.Released] });

        _mockCaseRepository
            .Setup(x => x.ExpireManyAsync(It.IsAny<IReadOnlyCollection<CaseId>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([expiredCase]);

        _mockWebhookConfigurationService
            .Setup(x => x.GetAllAsync(It.IsAny<OrganizationId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([FakeWebhookConfiguration.CreateValid(Fixture)]);

        // Act
        await _sut.ExpireDueAsync(CancellationToken.None);

        // Assert
        _mockAlertRepository.Verify(x => x.CloseByCaseAsync(expiredCase.CaseId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResolveAsync_GivenTheLastOutstandingPayment_ResolvesTheCaseAndClosesItsAlerts()
    {
        // Arrange
        var decision = FakeDecision.CreateValid(Fixture);
        var resolveDecisionByIdRequest = new ResolveDecisionByIdRequest
        {
            DecisionId = decision.DecisionId,
            Verdict = CaseStatus.Approved,
            Disposition = CaseDisposition.FalsePositive,
            CaseReasonCodeId = null,
            Note = null,
            ResolvedByUserId = new ApplicationUserId(Guid.NewGuid())
        };

        SetupResolvableDecision(decision, [DecisionStatus.Released]);

        // Act
        await _sut.ResolveAsync(resolveDecisionByIdRequest, CancellationToken.None);

        // Assert
        _mockCaseRepository.Verify(
            x => x.ResolveAsync(decision.CaseId!.Value, It.Is<CaseClosure>(closure => closure.Status == CaseStatus.Approved), It.IsAny<CancellationToken>()),
            Times.Once);
        _mockAlertRepository.Verify(x => x.CloseByCaseAsync(decision.CaseId!.Value, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResolveAsync_GivenAnotherPaymentStillHeld_LeavesTheCaseOpen()
    {
        // Arrange
        var decision = FakeDecision.CreateValid(Fixture);
        var resolveDecisionByIdRequest = new ResolveDecisionByIdRequest
        {
            DecisionId = decision.DecisionId,
            Verdict = CaseStatus.Approved,
            Disposition = CaseDisposition.FalsePositive,
            CaseReasonCodeId = null,
            Note = null,
            ResolvedByUserId = new ApplicationUserId(Guid.NewGuid())
        };

        SetupResolvableDecision(decision, [DecisionStatus.Held]);

        // Act
        await _sut.ResolveAsync(resolveDecisionByIdRequest, CancellationToken.None);

        // Assert
        _mockCaseRepository.Verify(
            x => x.ResolveAsync(It.IsAny<CaseId>(), It.IsAny<CaseClosure>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _mockAlertRepository.Verify(x => x.CloseByCaseAsync(It.IsAny<CaseId>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ResolveAsync_GivenABlockedPaymentUnderTheCase_RejectsTheCase()
    {
        // Arrange
        var decision = FakeDecision.CreateValid(Fixture);
        var resolveDecisionByIdRequest = new ResolveDecisionByIdRequest
        {
            DecisionId = decision.DecisionId,
            Verdict = CaseStatus.Approved,
            Disposition = CaseDisposition.FalsePositive,
            CaseReasonCodeId = null,
            Note = null,
            ResolvedByUserId = new ApplicationUserId(Guid.NewGuid())
        };

        SetupResolvableDecision(decision, [DecisionStatus.Blocked]);

        // Act
        await _sut.ResolveAsync(resolveDecisionByIdRequest, CancellationToken.None);

        // Assert
        _mockCaseRepository.Verify(
            x => x.ResolveAsync(decision.CaseId!.Value, It.Is<CaseClosure>(closure => closure.Status == CaseStatus.Rejected), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ResolveAsync_GivenTheCaseWasDeleted_SettlesThePaymentWithoutClosingTheCase()
    {
        // Arrange
        var decision = FakeDecision.CreateValid(Fixture);
        var resolveDecisionByIdRequest = new ResolveDecisionByIdRequest
        {
            DecisionId = decision.DecisionId,
            Verdict = CaseStatus.Approved,
            Disposition = CaseDisposition.FalsePositive,
            CaseReasonCodeId = null,
            Note = null,
            ResolvedByUserId = new ApplicationUserId(Guid.NewGuid())
        };

        SetupResolvableDecision(decision, []);

        _mockCaseRepository
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<CaseId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(false));

        // Act
        await _sut.ResolveAsync(resolveDecisionByIdRequest, CancellationToken.None);

        // Assert
        _mockCaseRepository.Verify(
            x => x.ResolveAsync(It.IsAny<CaseId>(), It.IsAny<CaseClosure>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private void SetupResolvableDecision(Decision decision, List<DecisionStatus> otherStatuses)
    {
        _mockDecisionRepository
            .Setup(x => x.ResolveAsync(It.IsAny<DecisionId>(), It.IsAny<DecisionVerdict>(), It.IsAny<ApplicationUserId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(decision);

        _mockDecisionRepository
            .Setup(x => x.ListStatusesByCaseAsync(It.IsAny<IReadOnlyCollection<CaseId>>(), It.IsAny<IReadOnlyCollection<DecisionId>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<CaseId, List<DecisionStatus>> { [decision.CaseId!.Value] = otherStatuses });

        _mockCaseRepository
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<CaseId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        _mockCaseRepository
            .Setup(x => x.ResolveAsync(It.IsAny<CaseId>(), It.IsAny<CaseClosure>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakeCase.CreateValid(Fixture));

        _mockWebhookConfigurationService
            .Setup(x => x.GetAllAsync(It.IsAny<OrganizationId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([FakeWebhookConfiguration.CreateValid(Fixture)]);
    }
}
