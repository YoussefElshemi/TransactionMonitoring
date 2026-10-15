using System.Text;
using Core.Constants;
using Core.Enums;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Models.Alerts;
using Core.Models.Common;
using Core.Models.OutboxMessages;
using Core.Services;
using Core.ValueObjects;
using FluentAssertions;
using Moq;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Alerts;
using UnitTests.TestHelpers.FakeObjects.Core.Models.WebhookConfigurations;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.Tests.Core.Services;

public class AlertServiceTests : BaseTestClass
{
    private readonly Mock<IAlertRepository> _mockAlertRepository = new();
    private readonly Mock<IOutboxMessageService> _mockOutboxMessageService = new();
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();
    private readonly Mock<IWebhookConfigurationService> _mockWebhookConfigurationService = new();
    private readonly AlertService _sut;

    public AlertServiceTests()
    {
        _mockAlertRepository
            .Setup(x => x.GenerateFriendlyName(It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakeFriendlyName.CreateValid(Fixture));

        _mockWebhookConfigurationService
            .Setup(x => x.GetAllAsync(It.IsAny<OrganizationId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([FakeWebhookConfiguration.CreateValid(Fixture)]);

        _sut = new AlertService(
            _mockAlertRepository.Object,
            _mockWebhookConfigurationService.Object,
            _mockOutboxMessageService.Object,
            _mockUnitOfWork.Object,
            MockTimeProvider,
            TestModeContext);
    }

    [Fact]
    public async Task PrepareAsync_NoDuplicateInWindow_RaisesAnOpenAlert()
    {
        // Arrange
        var request = Request(FakeAlertCandidate.CreateValid(Fixture));

        _mockAlertRepository
            .Setup(x => x.FindDuplicateAsync(It.IsAny<FindDuplicateAlertRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Alert?)null);

        _mockAlertRepository
            .Setup(x => x.FindGroupingCaseAsync(It.IsAny<FindGroupingCaseRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CaseId?)null);

        // Act
        var draft = await _sut.PrepareAsync(request, CancellationToken.None);

        // Assert
        draft.Alerts.Should().ContainSingle();
        draft.Alerts[0].Status.Should().Be(AlertStatus.Open);
        draft.Alerts[0].SuppressionReason.Should().BeNull();
        ((bool)draft.AllSuppressed).Should().BeFalse();
        draft.CaseId.Should().BeNull();
    }

    [Fact]
    public async Task PrepareAsync_DuplicateInWindow_SuppressesAndPointsAtTheWorkItFoldedInto()
    {
        // Arrange
        var caseId = FakeCaseId.CreateValid(Fixture);
        var duplicate = FakeAlert.CreateValid(Fixture) with { CaseId = caseId };
        var request = Request(FakeAlertCandidate.CreateValid(Fixture));

        _mockAlertRepository
            .Setup(x => x.FindDuplicateAsync(It.IsAny<FindDuplicateAlertRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(duplicate);

        // Act
        var draft = await _sut.PrepareAsync(request, CancellationToken.None);

        // Assert
        draft.Alerts[0].Status.Should().Be(AlertStatus.Suppressed);
        draft.Alerts[0].SuppressionReason.Should().Be(AlertSuppressionReason.DuplicateInWindow);
        draft.Alerts[0].SuppressedByAlertId.Should().Be(duplicate.AlertId);
        draft.Alerts[0].ClosedAt.Should().NotBeNull();
        draft.Alerts[0].CaseId.Should().Be(caseId);
        ((bool)draft.AllSuppressed).Should().BeTrue();
        draft.CaseId.Should().Be(caseId);
    }

    [Fact]
    public async Task PrepareAsync_SubjectHasAnOpenCaseInThePeriod_GroupsOntoIt()
    {
        // Arrange
        var caseId = FakeCaseId.CreateValid(Fixture);
        var request = Request(FakeAlertCandidate.CreateValid(Fixture));

        _mockAlertRepository
            .Setup(x => x.FindDuplicateAsync(It.IsAny<FindDuplicateAlertRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Alert?)null);

        _mockAlertRepository
            .Setup(x => x.FindGroupingCaseAsync(It.IsAny<FindGroupingCaseRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(caseId);

        // Act
        var draft = await _sut.PrepareAsync(request, CancellationToken.None);

        // Assert
        draft.Alerts[0].Status.Should().Be(AlertStatus.Open);
        draft.CaseId.Should().Be(caseId);
        ((bool)draft.AllSuppressed).Should().BeFalse();
    }

    [Fact]
    public async Task PrepareAsync_CandidateWithoutASubject_NeverProbesForADuplicate()
    {
        // Arrange
        var request = Request(FakeAlertCandidate.CreateValid(Fixture) with { PartyId = null, PartyRole = null });

        // Act
        var draft = await _sut.PrepareAsync(request, CancellationToken.None);

        // Assert
        draft.Alerts[0].Status.Should().Be(AlertStatus.Open);
        draft.CaseId.Should().BeNull();

        _mockAlertRepository.Verify(
            x => x.FindDuplicateAsync(It.IsAny<FindDuplicateAlertRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockAlertRepository.Verify(
            x => x.FindGroupingCaseAsync(It.IsAny<FindGroupingCaseRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PrepareAsync_DeduplicationWindow_BoundsTheProbeFromTheRaisedAt()
    {
        // Arrange
        var candidate = FakeAlertCandidate.CreateValid(Fixture) with
        {
            AlertPolicy = FakeAlertPolicy.CreateValid(Fixture) with { DeduplicationWindow = new DeduplicationWindow(30) }
        };

        var raisedAt = new RaisedAt(MockTimeProvider.GetUtcNow());
        var request = Request(candidate) with { RaisedAt = raisedAt };

        // Act
        await _sut.PrepareAsync(request, CancellationToken.None);

        // Assert
        _mockAlertRepository.Verify(x => x.FindDuplicateAsync(
            It.Is<FindDuplicateAlertRequest>(probe =>
                probe.RaisedAfter == (DateTimeOffset)raisedAt - TimeSpan.FromMinutes(30) &&
                probe.RuleId == candidate.RuleId &&
                probe.PartyId == candidate.PartyId!.Value),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PrepareAsync_SeveralCandidates_RaisesTheMostSevereFirst()
    {
        // Arrange
        var low = FakeAlertCandidate.CreateValid(Fixture) with { Severity = RuleSeverity.Low };
        var high = FakeAlertCandidate.CreateValid(Fixture) with { Severity = RuleSeverity.High };

        var request = Request(low, high);

        // Act
        var draft = await _sut.PrepareAsync(request, CancellationToken.None);

        // Assert
        draft.Alerts.Should().HaveCount(2);
        draft.Alerts[0].Severity.Should().Be(RuleSeverity.High);
        draft.Alerts[1].Severity.Should().Be(RuleSeverity.Low);
    }

    [Fact]
    public async Task PrepareAsync_OneOfSeveralSuppressed_IsNotAllSuppressed()
    {
        // Arrange
        var suppressed = FakeAlertCandidate.CreateValid(Fixture) with { Severity = RuleSeverity.High };
        var raised = FakeAlertCandidate.CreateValid(Fixture) with { Severity = RuleSeverity.Low };

        _mockAlertRepository
            .Setup(x => x.FindDuplicateAsync(
                It.Is<FindDuplicateAlertRequest>(probe => probe.RuleId == suppressed.RuleId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakeAlert.CreateValid(Fixture) with { CaseId = null });

        _mockAlertRepository
            .Setup(x => x.FindDuplicateAsync(
                It.Is<FindDuplicateAlertRequest>(probe => probe.RuleId == raised.RuleId), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Alert?)null);

        // Act
        var draft = await _sut.PrepareAsync(Request(suppressed, raised), CancellationToken.None);

        // Assert
        ((bool)draft.AllSuppressed).Should().BeFalse();
        draft.Alerts.Should().Contain(x => x.Status == AlertStatus.Suppressed);
        draft.Alerts.Should().Contain(x => x.Status == AlertStatus.Open);
    }

    [Fact]
    public async Task RaiseAsync_NoAlerts_DoesNotTouchTheRepository()
    {
        // Act
        await _sut.RaiseAsync([], FakeCaseId.CreateValid(Fixture), CancellationToken.None);

        // Assert
        _mockAlertRepository.Verify(
            x => x.AddRangeAsync(It.IsAny<List<Alert>>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RaiseAsync_StampsTheCaseOnOpenAlertsOnly()
    {
        // Arrange
        var foldedInto = FakeCaseId.CreateValid(Fixture);
        var caseId = FakeCaseId.CreateValid(Fixture);

        var open = FakeAlert.CreateValid(Fixture) with { Status = AlertStatus.Open, CaseId = null };
        var suppressed = FakeAlert.CreateValid(Fixture) with { Status = AlertStatus.Suppressed, CaseId = foldedInto };

        // Act
        var raised = await _sut.RaiseAsync([open, suppressed], caseId, CancellationToken.None);

        // Assert
        raised.Should().Contain(x => x.AlertId == open.AlertId && x.CaseId == caseId);
        raised.Should().Contain(x => x.AlertId == suppressed.AlertId && x.CaseId == foldedInto);

        _mockOutboxMessageService.Verify(
            x => x.StageManyAsync(It.IsAny<List<CreateOutboxMessageRequest>>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task RaiseAsync_ReadsWebhookConfigurationsOncePerBatch()
    {
        // Arrange
        var organizationId = FakeOrganizationId.CreateValid(Fixture);

        var alerts = new List<Alert>
        {
            FakeAlert.CreateValid(Fixture) with { OrganizationId = organizationId, Status = AlertStatus.Open },
            FakeAlert.CreateValid(Fixture) with { OrganizationId = organizationId, Status = AlertStatus.Open },
            FakeAlert.CreateValid(Fixture) with { OrganizationId = organizationId, Status = AlertStatus.Open }
        };

        // Act
        await _sut.RaiseAsync(alerts, FakeCaseId.CreateValid(Fixture), CancellationToken.None);

        // Assert
        _mockWebhookConfigurationService.Verify(
            x => x.GetAllAsync(It.IsAny<OrganizationId>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RaiseAsync_PublishesRaisedAndSuppressedSeparately()
    {
        // Arrange
        var organizationId = FakeOrganizationId.CreateValid(Fixture);
        var events = new List<string>();

        _mockOutboxMessageService
            .Setup(x => x.StageManyAsync(It.IsAny<List<CreateOutboxMessageRequest>>(), It.IsAny<CancellationToken>()))
            .Callback<List<CreateOutboxMessageRequest>, CancellationToken>((requests, _) => events.AddRange(requests.Select(y => DecodePayload(y))))
            .Returns(Task.CompletedTask);

        var alerts = new List<Alert>
        {
            FakeAlert.CreateValid(Fixture) with { OrganizationId = organizationId, Status = AlertStatus.Open },
            FakeAlert.CreateValid(Fixture) with { OrganizationId = organizationId, Status = AlertStatus.Suppressed }
        };

        // Act
        await _sut.RaiseAsync(alerts, FakeCaseId.CreateValid(Fixture), CancellationToken.None);

        // Assert
        events.Should().Contain(x => x.Contains(WebhookEvents.AlertRaised));
        events.Should().Contain(x => x.Contains(WebhookEvents.AlertSuppressed));
    }

    private static string DecodePayload(CreateOutboxMessageRequest createOutboxMessageRequest)
    {
        return Encoding.UTF8.GetString(Convert.FromBase64String(createOutboxMessageRequest.Payload));
    }

    [Fact]
    public async Task CloseByCaseAsync_ClosesTheAlertsAndCommits()
    {
        // Arrange
        var caseId = FakeCaseId.CreateValid(Fixture);

        // Act
        await _sut.CloseByCaseAsync(caseId, CancellationToken.None);

        // Assert
        _mockAlertRepository.Verify(x => x.CloseByCaseAsync(caseId, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAsync_ReturnsTheAlert()
    {
        // Arrange
        var alert = FakeAlert.CreateValid(Fixture);

        _mockAlertRepository
            .Setup(x => x.GetAsync(It.IsAny<AlertId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(alert);

        // Act
        var result = await _sut.GetAsync(alert.AlertId, CancellationToken.None);

        // Assert
        result.Should().Be(alert);
        _mockAlertRepository.Verify(x => x.GetAsync(alert.AlertId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExistsAsync_PassesTheDeletedFlagThrough()
    {
        // Arrange
        var organizationId = FakeOrganizationId.CreateValid(Fixture);
        var alertId = FakeAlertId.CreateValid(Fixture);
        var includeDeleted = new IncludeDeleted(true);

        _mockAlertRepository
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<AlertId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        // Act
        var exists = await _sut.ExistsAsync(organizationId, alertId, includeDeleted, CancellationToken.None);

        // Assert
        ((bool)exists).Should().BeTrue();
        _mockAlertRepository.Verify(
            x => x.ExistsAsync(organizationId, alertId, includeDeleted, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListAsync_PassesTheFilterThrough()
    {
        // Arrange
        var listAlertRequest = new ListAlertRequest
        {
            OrganizationId = FakeOrganizationId.CreateValid(Fixture),
            FriendlyName = null,
            Status = AlertStatus.Open,
            Severity = RuleSeverity.Critical,
            RuleId = null,
            PartyId = null,
            CaseId = null,
            PageNumber = new PageNumber(1),
            PageSize = new PageSize(10),
            FromDate = new DateRange(DateOnly.MinValue),
            ToDate = new DateRange(DateOnly.MaxValue)
        };

        _mockAlertRepository
            .Setup(x => x.ListAsync(It.IsAny<ListAlertRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResponse<Alert>
            {
                Data = [FakeAlert.CreateValid(Fixture)],
                PageNumber = new PageNumber(1),
                PageSize = new PageSize(10),
                PageCount = new PageCount(1),
                TotalCount = new TotalCount(1),
                HasMore = new HasMore(false)
            });

        // Act
        var result = await _sut.ListAsync(listAlertRequest, CancellationToken.None);

        // Assert
        result.Data.Should().HaveCount(1);
        _mockAlertRepository.Verify(x => x.ListAsync(listAlertRequest, It.IsAny<CancellationToken>()), Times.Once);
    }

    private RaiseAlertsRequest Request(params AlertCandidate[] candidates)
    {
        return new RaiseAlertsRequest
        {
            OrganizationId = FakeOrganizationId.CreateValid(Fixture),
            TransactionId = FakeTransactionId.CreateValid(Fixture),
            RaisedAt = new RaisedAt(MockTimeProvider.GetUtcNow()),
            Candidates = [..candidates]
        };
    }
}
