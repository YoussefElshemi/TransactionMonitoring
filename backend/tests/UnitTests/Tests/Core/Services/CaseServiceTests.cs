using System.Text.Json.Nodes;
using Core.Configs;
using Core.Enums;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Mappers;
using Core.Models.Approvals;
using Core.Models.CaseNotes;
using Core.Models.Cases;
using Core.Models.Decisions;
using Core.Models.OutboxMessages;
using Core.Models.Queues;
using Core.Services;
using Core.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Approvals;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Cases;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Decisions;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Localization;
using UnitTests.TestHelpers.FakeObjects.Core.Models.WebhookConfigurations;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.Tests.Core.Services;

public class CaseServiceTests : BaseTestClass
{
    private readonly Mock<ICaseRepository> _mockCaseRepository = new();
    private readonly Mock<ICaseSlaTargetRepository> _mockCaseSlaTargetRepository = new();
    private readonly Mock<ICaseRuleLinkRepository> _mockCaseRuleLinkRepository = new();
    private readonly Mock<ICaseNoteRepository> _mockCaseNoteRepository = new();
    private readonly Mock<IDecisionRepository> _mockDecisionRepository = new();
    private readonly Mock<IAlertRepository> _mockAlertRepository = new();
    private readonly Mock<IApprovalRepository> _mockApprovalRepository = new();
    private readonly Mock<IApprovalPolicyRepository> _mockApprovalPolicyRepository = new();
    private readonly Mock<IPartyService> _mockPartyService = new();
    private readonly Mock<IQueueRoutingService> _mockQueueRoutingService = new();
    private readonly Mock<IOutboxMessageService> _mockOutboxMessageService = new();
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();
    private readonly Mock<IDisplayPreferenceService> _mockDisplayPreferenceService = new();
    private readonly Mock<IWebhookConfigurationService> _mockWebhookConfigurationService = new();
    private readonly Mock<INotificationSubscriptionService> _mockNotificationSubscriptionService = new();
    private readonly CaseService _sut;

    public CaseServiceTests()
    {
        // The real repository returns an empty list; an unconfigured mock would return null.
        _mockNotificationSubscriptionService
            .Setup(x => x.ListEnabledAsync(It.IsAny<OrganizationId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _mockDisplayPreferenceService
            .Setup(x => x.ForOrganizationAsync(It.IsAny<OrganizationId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakeDisplayPreferences.CreateValid());

        _sut = new CaseService(
            _mockCaseRepository.Object,
            _mockCaseSlaTargetRepository.Object,
            _mockCaseRuleLinkRepository.Object,
            _mockCaseNoteRepository.Object,
            _mockDecisionRepository.Object,
            _mockAlertRepository.Object,
            _mockApprovalRepository.Object,
            _mockApprovalPolicyRepository.Object,
            _mockPartyService.Object,
            _mockQueueRoutingService.Object,
            _mockWebhookConfigurationService.Object,
            _mockNotificationSubscriptionService.Object,
            _mockOutboxMessageService.Object,
            _mockDisplayPreferenceService.Object,
            _mockUnitOfWork.Object,
            Options.Create(new AppConfig { CaseConfig = new CaseConfig() }),
            MockTimeProvider,
            TestModeContext);

        _mockCaseSlaTargetRepository
            .Setup(x => x.GetAsync(It.IsAny<OrganizationId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _mockQueueRoutingService
            .Setup(x => x.RouteAsync(It.IsAny<RouteCaseRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QueueRouting { QueueId = null, AssignedToUserId = null });

        _mockCaseRepository
            .Setup(x => x.GetAsync(It.IsAny<CaseId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakeCase.CreateValid(Fixture));

        _mockWebhookConfigurationService
            .Setup(x => x.GetAllAsync(It.IsAny<OrganizationId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _mockDecisionRepository
            .Setup(x => x.ResolveByCaseAsync(It.IsAny<ResolveDecisionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
    }

    [Fact]
    public async Task CreateAsync_GivenCreateCaseRequest_CreatesCase_RuleLinks_SendsWebhook()
    {
        // Arrange
        var ruleEvaluationId = FakeRuleEvaluationId.CreateValid(Fixture);
        var friendlyName = FakeFriendlyName.CreateValid(Fixture);
        var createCaseRequest = FakeCreateCaseRequest.CreateValid(Fixture) with
        {
            RuleEvaluationIds = [ruleEvaluationId]
        };

        _mockCaseRepository
            .Setup(x => x.GenerateFriendlyName(It.IsAny<CancellationToken>()))
            .ReturnsAsync(friendlyName);

        _mockCaseRepository
            .Setup(x => x.GetAsync(It.IsAny<CaseId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createCaseRequest.ToModel(new Dictionary<RuleSeverity, SlaTarget>(), new CaseConfig(), MockTimeProvider, Mode.Live) with
            {
                FriendlyName = friendlyName
            });

        _mockWebhookConfigurationService
            .Setup(x => x.GetAllAsync(It.IsAny<OrganizationId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([FakeWebhookConfiguration.CreateValid(Fixture)]);

        // Act
        var @case = await _sut.CreateAsync(createCaseRequest, CancellationToken.None);

        // Assert
        @case.Should().NotBeNull();
        @case.FriendlyName.Should().Be(friendlyName);
        @case.OrganizationId.Should().Be(createCaseRequest.OrganizationId);

        _mockCaseRepository.Verify(x => x.GenerateFriendlyName(It.IsAny<CancellationToken>()), Times.Once);
        _mockCaseRuleLinkRepository.Verify(x => x.AddRangeAsync(It.IsAny<List<CaseRuleEvaluationLink>>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockCaseRepository.Verify(x => x.GetAsync(It.IsAny<CaseId>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockWebhookConfigurationService.Verify(x => x.GetAllAsync(It.IsAny<OrganizationId>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockOutboxMessageService.Verify(x => x.CreateManyAsync(It.IsAny<List<CreateOutboxMessageRequest>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResolveAsync_GivenResolvedCase_WritesDecisionAndStagesBothWebhooksInOneSave()
    {
        // Arrange
        var resolveCaseRequest = new ResolveCaseRequest
        {
            CaseId = FakeCaseId.CreateValid(Fixture),
            OrganizationId = FakeOrganizationId.CreateValid(Fixture),
            Status = CaseStatus.Approved,
            Disposition = CaseDisposition.FalsePositive,
            CaseReasonCodeId = null,
            Note = null,
            ResolvedByUserId = FakeApplicationUserId.CreateValid(Fixture)
        };

        _mockCaseRepository
            .Setup(x => x.ResolveAsync(It.IsAny<CaseId>(), It.IsAny<CaseClosure>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakeCase.CreateValid(Fixture));

        _mockDecisionRepository
            .Setup(x => x.ResolveByCaseAsync(It.IsAny<ResolveDecisionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([FakeDecision.CreateValid(Fixture)]);

        _mockWebhookConfigurationService
            .Setup(x => x.GetAllAsync(It.IsAny<OrganizationId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([FakeWebhookConfiguration.CreateValid(Fixture)]);

        // Act
        await _sut.ResolveAsync(resolveCaseRequest, CancellationToken.None);

        // Assert
        _mockDecisionRepository.Verify(x => x.ResolveByCaseAsync(
            It.Is<ResolveDecisionRequest>(request =>
                request.CaseId == resolveCaseRequest.CaseId &&
                request.Verdict.Status == DecisionStatus.Released &&
                request.Verdict.ReasonCode == DecisionReasonCode.AnalystApproved &&
                request.ResolvedByUserId == resolveCaseRequest.ResolvedByUserId),
            It.IsAny<CancellationToken>()), Times.Once);

        _mockOutboxMessageService.Verify(x => x.StageManyAsync(It.IsAny<List<CreateOutboxMessageRequest>>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _mockOutboxMessageService.Verify(x => x.CreateManyAsync(It.IsAny<List<CreateOutboxMessageRequest>>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResolveAsync_WithNoApprovalPolicy_ClosesTheCaseWithoutReadingIt()
    {
        // Arrange
        var resolveCaseRequest = new ResolveCaseRequest
        {
            CaseId = FakeCaseId.CreateValid(Fixture),
            OrganizationId = FakeOrganizationId.CreateValid(Fixture),
            Status = CaseStatus.Approved,
            Disposition = CaseDisposition.FalsePositive,
            CaseReasonCodeId = null,
            Note = null,
            ResolvedByUserId = FakeApplicationUserId.CreateValid(Fixture)
        };

        _mockCaseRepository
            .Setup(x => x.ResolveAsync(It.IsAny<CaseId>(), It.IsAny<CaseClosure>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakeCase.CreateValid(Fixture));

        // Act
        var approval = await _sut.ResolveAsync(resolveCaseRequest, CancellationToken.None);

        // Assert
        approval.Should().BeNull();

        _mockApprovalPolicyRepository.Verify(x => x.FindAsync(resolveCaseRequest.OrganizationId, It.IsAny<CancellationToken>()), Times.Once);
        _mockCaseRepository.Verify(x => x.GetAsync(It.IsAny<CaseId>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockApprovalRepository.Verify(x => x.AddAsync(It.IsAny<Approval>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockCaseRepository.Verify(x => x.ResolveAsync(It.IsAny<CaseId>(), It.IsAny<CaseClosure>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResolveAsync_AboveTheSeverityFloor_RaisesARequestAndLeavesTheCaseOpen()
    {
        // Arrange
        var resolveCaseRequest = new ResolveCaseRequest
        {
            CaseId = FakeCaseId.CreateValid(Fixture),
            OrganizationId = FakeOrganizationId.CreateValid(Fixture),
            Status = CaseStatus.Approved,
            Disposition = CaseDisposition.FalsePositive,
            CaseReasonCodeId = null,
            Note = FakeMessage.CreateValid(Fixture),
            ResolvedByUserId = FakeApplicationUserId.CreateValid(Fixture)
        };

        _mockApprovalPolicyRepository
            .Setup(x => x.FindAsync(It.IsAny<OrganizationId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakeApprovalPolicy.CreateValid(Fixture) with { CaseDecisionMinimumSeverity = RuleSeverity.High });

        _mockCaseRepository
            .Setup(x => x.GetAsync(It.IsAny<CaseId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakeCase.CreateValid(Fixture) with { Severity = RuleSeverity.Critical });

        Approval? added = null;

        _mockApprovalRepository
            .Setup(x => x.AddAsync(It.IsAny<Approval>(), It.IsAny<CancellationToken>()))
            .Callback<Approval, CancellationToken>((approval, _) => added = approval);

        // Act
        var approval = await _sut.ResolveAsync(resolveCaseRequest, CancellationToken.None);

        // Assert
        approval.Should().NotBeNull();
        _mockCaseRepository.Verify(x => x.ResolveAsync(It.IsAny<CaseId>(), It.IsAny<CaseClosure>(), It.IsAny<CancellationToken>()), Times.Never);

        added.Should().NotBeNull();
        added!.Subject.Should().Be(ApprovalSubject.CaseResolution);
        added.Status.Should().Be(ApprovalStatus.Pending);
        added.RequestedByUserId.Should().Be(resolveCaseRequest.ResolvedByUserId);
        added.ProposedCaseResolution!.Note.Should().Be(resolveCaseRequest.Note);

        _mockCaseNoteRepository.Verify(x => x.AddAsync(It.IsAny<CaseNote>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ResolveAsync_WithASeverityOnlyPolicy_NeverReadsThePayload()
    {
        // Arrange
        var resolveCaseRequest = new ResolveCaseRequest
        {
            CaseId = FakeCaseId.CreateValid(Fixture),
            OrganizationId = FakeOrganizationId.CreateValid(Fixture),
            Status = CaseStatus.Approved,
            Disposition = CaseDisposition.FalsePositive,
            CaseReasonCodeId = null,
            Note = null,
            ResolvedByUserId = FakeApplicationUserId.CreateValid(Fixture)
        };

        _mockApprovalPolicyRepository
            .Setup(x => x.FindAsync(It.IsAny<OrganizationId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakeApprovalPolicy.CreateValid(Fixture) with { CaseDecisionMinimumSeverity = RuleSeverity.High });

        _mockCaseRepository
            .Setup(x => x.GetAsync(It.IsAny<CaseId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakeCase.CreateValid(Fixture) with { Severity = RuleSeverity.Low });

        _mockCaseRepository
            .Setup(x => x.ResolveAsync(It.IsAny<CaseId>(), It.IsAny<CaseClosure>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakeCase.CreateValid(Fixture));

        // Act
        var approval = await _sut.ResolveAsync(resolveCaseRequest, CancellationToken.None);

        // Assert
        approval.Should().BeNull();

        _mockCaseRepository.Verify(x => x.GetPayloadAsync(It.IsAny<CaseId>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockPartyService.Verify(x => x.PeekAsync(It.IsAny<OrganizationId>(), It.IsAny<JsonObject>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ApplyApprovedResolutionAsync_ClosesTheCaseWithoutConsultingThePolicy()
    {
        // Arrange
        var resolveCaseRequest = new ResolveCaseRequest
        {
            CaseId = FakeCaseId.CreateValid(Fixture),
            OrganizationId = FakeOrganizationId.CreateValid(Fixture),
            Status = CaseStatus.Approved,
            Disposition = CaseDisposition.FalsePositive,
            CaseReasonCodeId = null,
            Note = null,
            ResolvedByUserId = FakeApplicationUserId.CreateValid(Fixture)
        };

        _mockApprovalPolicyRepository
            .Setup(x => x.FindAsync(It.IsAny<OrganizationId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakeApprovalPolicy.CreateValid(Fixture) with { CaseDecisionMinimumSeverity = RuleSeverity.Info });

        _mockCaseRepository
            .Setup(x => x.ResolveAsync(It.IsAny<CaseId>(), It.IsAny<CaseClosure>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakeCase.CreateValid(Fixture));

        // Act
        await _sut.ApplyApprovedResolutionAsync(resolveCaseRequest, CancellationToken.None);

        // Assert
        _mockCaseRepository.Verify(x => x.ResolveAsync(It.IsAny<CaseId>(), It.IsAny<CaseClosure>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockApprovalPolicyRepository.Verify(x => x.FindAsync(It.IsAny<OrganizationId>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockApprovalRepository.Verify(x => x.AddAsync(It.IsAny<Approval>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ResolveAsync_GivenCaseWithNoResolvableDecisions_StagesOnlyCaseWebhook()
    {
        // Arrange
        var resolveCaseRequest = new ResolveCaseRequest
        {
            CaseId = FakeCaseId.CreateValid(Fixture),
            OrganizationId = FakeOrganizationId.CreateValid(Fixture),
            Status = CaseStatus.Rejected,
            Disposition = CaseDisposition.TruePositive,
            CaseReasonCodeId = null,
            Note = FakeMessage.CreateValid(Fixture),
            ResolvedByUserId = FakeApplicationUserId.CreateValid(Fixture)
        };

        _mockCaseRepository
            .Setup(x => x.ResolveAsync(It.IsAny<CaseId>(), It.IsAny<CaseClosure>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakeCase.CreateValid(Fixture));

        _mockDecisionRepository
            .Setup(x => x.ResolveByCaseAsync(It.IsAny<ResolveDecisionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _mockWebhookConfigurationService
            .Setup(x => x.GetAllAsync(It.IsAny<OrganizationId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([FakeWebhookConfiguration.CreateValid(Fixture)]);

        // Act
        await _sut.ResolveAsync(resolveCaseRequest, CancellationToken.None);

        // Assert
        _mockOutboxMessageService.Verify(x => x.StageManyAsync(It.IsAny<List<CreateOutboxMessageRequest>>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResolveAsync_GivenANote_WritesItInTheSameSaveAsTheClose()
    {
        // Arrange
        var resolveCaseRequest = new ResolveCaseRequest
        {
            CaseId = FakeCaseId.CreateValid(Fixture),
            OrganizationId = FakeOrganizationId.CreateValid(Fixture),
            Status = CaseStatus.Rejected,
            Disposition = CaseDisposition.TruePositive,
            CaseReasonCodeId = FakeCaseReasonCodeId.CreateValid(Fixture),
            Note = FakeMessage.CreateValid(Fixture),
            ResolvedByUserId = FakeApplicationUserId.CreateValid(Fixture)
        };

        var @case = FakeCase.CreateValid(Fixture);

        _mockCaseRepository
            .Setup(x => x.ResolveAsync(It.IsAny<CaseId>(), It.IsAny<CaseClosure>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(@case);

        _mockDecisionRepository
            .Setup(x => x.ResolveByCaseAsync(It.IsAny<ResolveDecisionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _mockWebhookConfigurationService
            .Setup(x => x.GetAllAsync(It.IsAny<OrganizationId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([FakeWebhookConfiguration.CreateValid(Fixture)]);

        // Act
        await _sut.ResolveAsync(resolveCaseRequest, CancellationToken.None);

        // Assert
        _mockCaseRepository.Verify(x => x.ResolveAsync(
            resolveCaseRequest.CaseId,
            It.Is<CaseClosure>(closure =>
                closure.Status == CaseStatus.Rejected &&
                closure.Disposition == CaseDisposition.TruePositive &&
                closure.CaseReasonCodeId == resolveCaseRequest.CaseReasonCodeId),
            It.IsAny<CancellationToken>()), Times.Once);

        _mockCaseNoteRepository.Verify(x => x.AddAsync(
            It.Is<CaseNote>(note =>
                note.CaseId == @case.CaseId &&
                note.CreatedByUserId == resolveCaseRequest.ResolvedByUserId),
            It.IsAny<CancellationToken>()), Times.Once);

        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResolveAsync_GivenNoNote_WritesNone()
    {
        // Arrange
        var resolveCaseRequest = new ResolveCaseRequest
        {
            CaseId = FakeCaseId.CreateValid(Fixture),
            OrganizationId = FakeOrganizationId.CreateValid(Fixture),
            Status = CaseStatus.Approved,
            Disposition = CaseDisposition.FalsePositive,
            CaseReasonCodeId = null,
            Note = null,
            ResolvedByUserId = FakeApplicationUserId.CreateValid(Fixture)
        };

        _mockCaseRepository
            .Setup(x => x.ResolveAsync(It.IsAny<CaseId>(), It.IsAny<CaseClosure>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakeCase.CreateValid(Fixture));

        _mockDecisionRepository
            .Setup(x => x.ResolveByCaseAsync(It.IsAny<ResolveDecisionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _mockWebhookConfigurationService
            .Setup(x => x.GetAllAsync(It.IsAny<OrganizationId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([FakeWebhookConfiguration.CreateValid(Fixture)]);

        // Act
        await _sut.ResolveAsync(resolveCaseRequest, CancellationToken.None);

        // Assert
        _mockCaseNoteRepository.Verify(x => x.AddAsync(It.IsAny<CaseNote>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ReopenAsync_ReopensTheAlertsAndLeavesTheDecisionsSettled()
    {
        // Arrange
        var reopenCaseRequest = new ReopenCaseRequest
        {
            CaseId = FakeCaseId.CreateValid(Fixture),
            Reason = FakeMessage.CreateValid(Fixture),
            ReopenedByUserId = FakeApplicationUserId.CreateValid(Fixture)
        };

        _mockCaseRepository
            .Setup(x => x.ReopenAsync(It.IsAny<CaseId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakeCase.CreateValid(Fixture));

        _mockWebhookConfigurationService
            .Setup(x => x.GetAllAsync(It.IsAny<OrganizationId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([FakeWebhookConfiguration.CreateValid(Fixture)]);

        // Act
        await _sut.ReopenAsync(reopenCaseRequest, CancellationToken.None);

        // Assert
        _mockAlertRepository.Verify(x => x.ReopenByCaseAsync(reopenCaseRequest.CaseId, It.IsAny<CancellationToken>()), Times.Once);
        _mockCaseNoteRepository.Verify(x => x.AddAsync(It.IsAny<CaseNote>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockDecisionRepository.Verify(x => x.ResolveByCaseAsync(It.IsAny<ResolveDecisionRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ChangeStatusAsync_MovesTheCaseAndStagesItsWebhook()
    {
        // Arrange
        var changeCaseStatusRequest = new ChangeCaseStatusRequest
        {
            CaseId = FakeCaseId.CreateValid(Fixture),
            Status = CaseStatus.Escalated,
            Note = null,
            ChangedByUserId = FakeApplicationUserId.CreateValid(Fixture)
        };

        _mockCaseRepository
            .Setup(x => x.ChangeStatusAsync(It.IsAny<CaseId>(), It.IsAny<CaseStatus>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakeCase.CreateValid(Fixture));

        _mockWebhookConfigurationService
            .Setup(x => x.GetAllAsync(It.IsAny<OrganizationId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([FakeWebhookConfiguration.CreateValid(Fixture)]);

        // Act
        await _sut.ChangeStatusAsync(changeCaseStatusRequest, CancellationToken.None);

        // Assert
        _mockCaseRepository.Verify(x => x.ChangeStatusAsync(changeCaseStatusRequest.CaseId, CaseStatus.Escalated, It.IsAny<CancellationToken>()), Times.Once);
        _mockOutboxMessageService.Verify(x => x.StageManyAsync(It.IsAny<List<CreateOutboxMessageRequest>>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
