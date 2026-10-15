using System.Text.Json.Nodes;
using Core.Configs;
using Core.Constants;
using Core.Enums;
using Core.Extensions;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Models.Escalations;
using Core.Models.OutboxMessages;
using Core.Models.Rules;
using Core.Services;
using Core.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Cases;
using UnitTests.TestHelpers.FakeObjects.Core.Models.WebhookConfigurations;

namespace UnitTests.Tests.Core.Services;

public class EscalationRuleServiceTests : BaseTestClass
{
    private readonly Mock<IEscalationRuleRepository> _mockEscalationRuleRepository = new();
    private readonly Mock<ICaseRepository> _mockCaseRepository = new();
    private readonly Mock<IPartyService> _mockPartyService = new();
    private readonly Mock<IWebhookConfigurationService> _mockWebhookConfigurationService = new();
    private readonly Mock<IOutboxMessageService> _mockOutboxMessageService = new();
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();
    private readonly OrganizationId _organizationId = new(Guid.NewGuid());
    private readonly EscalationRuleService _sut;

    public EscalationRuleServiceTests()
    {
        _sut = new EscalationRuleService(
            _mockEscalationRuleRepository.Object,
            _mockCaseRepository.Object,
            _mockPartyService.Object,
            _mockWebhookConfigurationService.Object,
            _mockOutboxMessageService.Object,
            _mockUnitOfWork.Object,
            Options.Create(new AppConfig { CaseConfig = new CaseConfig() }),
            MockTimeProvider,
            TestModeContext);

        _mockWebhookConfigurationService
            .Setup(x => x.GetAllAsync(It.IsAny<OrganizationId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([FakeWebhookConfiguration.CreateValid(Fixture)]);
    }

    [Fact]
    public async Task EscalateDueAsync_WithNoOrganizationsDeclaringRules_ReadsNoCases()
    {
        // Arrange
        _mockEscalationRuleRepository
            .Setup(x => x.ListOrganizationsWithActiveRulesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        var escalated = await _sut.EscalateDueAsync(CancellationToken.None);

        // Assert
        ((int)escalated).Should().Be(0);

        _mockCaseRepository.Verify(
            x => x.ListEscalationCandidatesAsync(It.IsAny<ListEscalationCandidateRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task EscalateDueAsync_GivenAMatchingRule_StampsTheCaseAndSendsTheWebhook()
    {
        // Arrange
        var escalationRule = Rule() with { MinimumSeverity = RuleSeverity.Info };
        var candidate = Candidate();

        SetUp(escalationRule, candidate);

        // Act
        var escalated = await _sut.EscalateDueAsync(CancellationToken.None);

        // Assert
        ((int)escalated).Should().Be(1);

        _mockCaseRepository.Verify(x => x.EscalateManyAsync(
            It.Is<IReadOnlyCollection<CaseEscalation>>(y =>
                y.Count == 1 &&
                y.First().CaseId == candidate.Case.CaseId &&
                y.First().EscalationRuleId == escalationRule.EscalationRuleId),
            It.IsAny<CancellationToken>()), Times.Once);

        _mockOutboxMessageService.Verify(x => x.StageManyAsync(
            It.Is<List<CreateOutboxMessageRequest>>(y => y.TrueForAll(z => z.Payload.ToString().FromBase64String().Contains(WebhookEvents.CaseEscalated))),
            It.IsAny<CancellationToken>()), Times.Once);

        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EscalateDueAsync_WithNothingMatching_WritesNothing()
    {
        // Arrange
        var escalationRule = Rule() with { MinimumSeverity = RuleSeverity.Critical };

        SetUp(escalationRule, Candidate(severity: RuleSeverity.Low));

        // Act
        var escalated = await _sut.EscalateDueAsync(CancellationToken.None);

        // Assert
        ((int)escalated).Should().Be(0);

        _mockCaseRepository.Verify(
            x => x.EscalateManyAsync(It.IsAny<IReadOnlyCollection<CaseEscalation>>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EscalateDueAsync_RaisingToALowerSeverity_LeavesTheSeverityAlone()
    {
        // Arrange
        var escalationRule = Rule() with
        {
            MinimumSeverity = RuleSeverity.Info,
            RaiseSeverityTo = RuleSeverity.Low
        };

        SetUp(escalationRule, Candidate(severity: RuleSeverity.High));

        // Act
        await _sut.EscalateDueAsync(CancellationToken.None);

        // Assert
        _mockCaseRepository.Verify(x => x.EscalateManyAsync(
            It.Is<IReadOnlyCollection<CaseEscalation>>(y => y.First().RaiseSeverityTo == null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EscalateDueAsync_RaisingToAHigherSeverity_CarriesTheRaise()
    {
        // Arrange
        var escalationRule = Rule() with
        {
            MinimumSeverity = RuleSeverity.Info,
            RaiseSeverityTo = RuleSeverity.Critical
        };

        SetUp(escalationRule, Candidate(severity: RuleSeverity.Medium));

        // Act
        await _sut.EscalateDueAsync(CancellationToken.None);

        // Assert
        _mockCaseRepository.Verify(x => x.EscalateManyAsync(
            It.Is<IReadOnlyCollection<CaseEscalation>>(y => y.First().RaiseSeverityTo == RuleSeverity.Critical),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EscalateDueAsync_TargetingTheQueueTheCaseIsAlreadyOn_RecordsNoMove()
    {
        // Arrange
        var queueId = new QueueId(Guid.NewGuid());
        var escalationRule = Rule() with
        {
            MinimumSeverity = RuleSeverity.Info,
            TargetQueueId = queueId
        };

        SetUp(escalationRule, Candidate(queueId: queueId));

        // Act
        await _sut.EscalateDueAsync(CancellationToken.None);

        // Assert
        _mockCaseRepository.Verify(x => x.EscalateManyAsync(
            It.Is<IReadOnlyCollection<CaseEscalation>>(y => y.First().TargetQueueId == null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EscalateDueAsync_WithNoRuleDeclaringAMatcher_DoesNotPeekParties()
    {
        // Arrange
        SetUp(Rule() with { MinimumSeverity = RuleSeverity.Info }, Candidate());

        // Act
        await _sut.EscalateDueAsync(CancellationToken.None);

        // Assert
        _mockPartyService.Verify(
            x => x.PeekManyAsync(It.IsAny<OrganizationId>(), It.IsAny<IReadOnlyList<JsonObject>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task EscalateDueAsync_WithAMatcher_PeeksThePartiesOnceForTheBatch()
    {
        // Arrange
        var escalationRule = Rule() with
        {
            Matcher = new RuleBody
            {
                Condition = RuleCondition.GreaterThan,
                Field = new RuleField("payment.amount"),
                Value = new RuleValue(10m),
                And = null,
                Or = null,
                Not = null
            }
        };

        SetUp(escalationRule, Candidate(), Candidate());

        _mockPartyService
            .Setup(x => x.PeekManyAsync(It.IsAny<OrganizationId>(), It.IsAny<IReadOnlyList<JsonObject>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([[], []]);

        // Act
        await _sut.EscalateDueAsync(CancellationToken.None);

        // Assert
        _mockPartyService.Verify(
            x => x.PeekManyAsync(It.IsAny<OrganizationId>(), It.IsAny<IReadOnlyList<JsonObject>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private void SetUp(EscalationRule escalationRule, params EscalationCandidate[] candidates)
    {
        _mockEscalationRuleRepository
            .Setup(x => x.ListOrganizationsWithActiveRulesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([_organizationId]);

        _mockEscalationRuleRepository
            .Setup(x => x.ListActiveAsync(It.IsAny<OrganizationId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([escalationRule]);

        _mockCaseRepository
            .SetupSequence(x => x.ListEscalationCandidatesAsync(It.IsAny<ListEscalationCandidateRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([..candidates])
            .ReturnsAsync([]);

        _mockCaseRepository
            .Setup(x => x.EscalateManyAsync(It.IsAny<IReadOnlyCollection<CaseEscalation>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<CaseEscalation> escalations, CancellationToken _) =>
                escalations.Select(escalation =>
                    candidates.First(candidate => candidate.Case.CaseId == escalation.CaseId).Case).ToList());
    }

    private EscalationRule Rule()
    {
        var now = MockTimeProvider.GetUtcNow();

        return new EscalationRule
        {
            Mode = Mode.Live,
            EscalationRuleId = new EscalationRuleId(Guid.NewGuid()),
            OrganizationId = _organizationId,
            Name = new EscalationRuleName("Escalation"),
            Description = null,
            Priority = new EscalationPriority(100),
            IsEnabled = new IsEnabled(true),
            UnassignedFor = null,
            HoldExpiryWithin = null,
            MinimumSeverity = null,
            Matcher = null,
            TargetQueueId = null,
            RaiseSeverityTo = null,
            CreatedAt = new CreatedAt(now.AddDays(-1)),
            UpdatedAt = new UpdatedAt(now.AddDays(-1)),
            IsDeleted = new IsDeleted(false),
            DeletedAt = null
        };
    }

    private EscalationCandidate Candidate(RuleSeverity severity = RuleSeverity.Medium, QueueId? queueId = null)
    {
        var now = MockTimeProvider.GetUtcNow();

        var @case = FakeCase.CreateValid(Fixture) with
        {
            OrganizationId = _organizationId,
            Status = CaseStatus.Pending,
            Severity = severity,
            QueueId = queueId,
            Queue = null,
            AssignedToUserId = null,
            AssignedToUser = null,
            CreatedAt = new CreatedAt(now.AddHours(-2)),
            DueAt = new DueAt(now.AddHours(1)),
            EscalatedAt = null,
            EscalationRuleId = null,
            ResolvedAt = null
        };

        return new EscalationCandidate
        {
            Case = @case,
            Payload = new JsonObject
            {
                ["payment"] = new JsonObject { ["amount"] = JsonValue.Create(100m) }
            },
            HoldExpiresAt = null
        };
    }
}
