using Core.Configs;
using Core.Enums;
using Core.Helpers;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Models.Alerts;
using Core.Models.Cases;
using Core.Models.Decisions;
using Core.Models.Parties;
using Core.Models.RuleDeployments;
using Core.Models.RuleEvaluations;
using Core.Models.Rules;
using Core.Models.Transactions;
using Core.Models.Usages;
using Core.Models.VelocityObservations;
using Core.Services;
using Core.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Cases;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Decisions;
using UnitTests.TestHelpers.FakeObjects.Core.Models.RuleDeployments;
using UnitTests.TestHelpers.FakeObjects.Core.Models.RuleEvaluations;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Rules;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Transactions;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.Tests.Core.Services;

public class RuleServiceTests : BaseTestClass
{
    private readonly Mock<ICaseService> _mockCaseService = new();
    private readonly Mock<IAlertService> _mockAlertService = new();
    private readonly Mock<IDecisionService> _mockDecisionService = new();
    private readonly Mock<IRuleEvaluationService> _mockRuleEvaluationService = new();
    private readonly Mock<IRuleRepository> _mockRuleRepository = new();
    private readonly Mock<IRuleVersionRepository> _mockRuleVersionRepository = new();
    private readonly Mock<IRuleDeploymentRepository> _mockRuleDeploymentRepository = new();
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();
    private readonly Mock<ISandboxCreditService> _mockSandboxCreditService = new();
    private readonly Mock<IUsageService> _mockUsageService = new();
    private readonly Mock<IRandomProvider> _mockRandomProvider = new();
    private readonly Mock<ITransactionService> _mockTransactionService = new();
    private readonly Mock<ITransactionSchemaService> _mockTransactionSchemaService = new();
    private readonly Mock<IPayloadProtectionService> _mockPayloadProtectionService = new();
    private readonly Mock<IRuleSetRepository> _mockRuleSetRepository = new();
    private readonly Mock<IPartyService> _mockPartyService = new();
    private readonly Mock<IPartyMappingService> _mockPartyMappingService = new();
    private readonly Mock<IVelocityService> _mockVelocityService = new();
    private readonly Mock<IVelocityProfileService> _mockVelocityProfileService = new();
    private readonly RuleService _sut;

    public RuleServiceTests()
    {
        var appConfig = Options.Create(new AppConfig
        {
            RuleConfig = new RuleConfig { DefaultReviewThreshold = 100 },
            DecisionConfig = new DecisionConfig()
        });

        _mockAlertService
            .Setup(x => x.PrepareAsync(It.IsAny<RaiseAlertsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AlertDraft
            {
                Alerts = [],
                CaseId = null,
                AllSuppressed = new AllSuppressed(false)
            });

        _mockPartyService
            .Setup(x => x.RecordAsync(It.IsAny<RecordTransactionPartiesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _mockVelocityService
            .Setup(x => x.PeekManyAsync(It.IsAny<OrganizationId>(), It.IsAny<IReadOnlyList<VelocityPeekSample>>(), It.IsAny<IReadOnlyList<RuleBodyModel>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OrganizationId _, IReadOnlyList<VelocityPeekSample> samples, IReadOnlyList<RuleBodyModel> _, CancellationToken _) =>
                samples.Select(_ => VelocityFacts.Empty).ToList());

        _mockVelocityService
            .Setup(x => x.RecordAndReadAsync(It.IsAny<RecordVelocityObservationsRequest>(), It.IsAny<IReadOnlyList<RuleBodyModel>>(), It.IsAny<EvaluatedAt>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(VelocityFacts.Empty);

        _mockPayloadProtectionService
            .Setup(x => x.GetOrCreateDataKeyAsync(It.IsAny<OrganizationId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(DataKeyWrapper.Create());

        _sut = new RuleService(
            _mockRuleRepository.Object,
            _mockRuleVersionRepository.Object,
            _mockRuleDeploymentRepository.Object,
            _mockRuleSetRepository.Object,
            _mockUsageService.Object,
            _mockCaseService.Object,
            _mockAlertService.Object,
            _mockDecisionService.Object,
            _mockRuleEvaluationService.Object,
            _mockTransactionService.Object,
            _mockTransactionSchemaService.Object,
            _mockPayloadProtectionService.Object,
            _mockPartyService.Object,
            _mockPartyMappingService.Object,
            _mockVelocityService.Object,
            _mockVelocityProfileService.Object,
            _mockUnitOfWork.Object,
            _mockRandomProvider.Object,
            appConfig,
            MockTimeProvider,
            _mockSandboxCreditService.Object,
            TestModeContext);

        _mockTransactionService
            .Setup(x => x.GetOrCreateAsync(It.IsAny<CreateTransactionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => FakeTransaction.CreateValid(Fixture));

        _mockRuleSetRepository
            .Setup(x => x.GetReviewThresholdAsync(It.IsAny<RuleSetId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ReviewThreshold?)null);

        _mockRuleEvaluationService
            .Setup(x => x.CreateAsync(It.IsAny<CreateRuleEvaluationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => FakeRuleEvaluation.CreateValid(Fixture));

        _mockUnitOfWork
            .Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task<(Case? Case, Decision Decision)>>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<Task<(Case? Case, Decision Decision)>> operation, CancellationToken _) => operation());
    }

    [Fact]
    public async Task UpdateAsync_UpdatesMetadataAndReturnsRule()
    {
        // Arrange
        var updateRuleRequest = FakeUpdateRuleRequest.CreateValid(Fixture);
        var rule = FakeRule.CreateValid(Fixture);

        _mockRuleRepository
            .Setup(x => x.GetAsync(It.IsAny<RuleId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rule);

        // Act
        var result = await _sut.UpdateAsync(updateRuleRequest, CancellationToken.None);

        // Assert
        result.Should().Be(rule);
        _mockRuleRepository.Verify(x => x.UpdateAsync(updateRuleRequest, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockRuleRepository.Verify(x => x.GetAsync(updateRuleRequest.RuleId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_CreatesRuleWithInitialVersion()
    {
        // Arrange
        var createRuleRequest = FakeCreateRuleRequest.CreateValid(Fixture);
        var rule = FakeRule.CreateValid(Fixture);

        Rule? addedRule = null;
        RuleVersion? addedVersion = null;
        RuleDeployment? addedDeployment = null;

        _mockRuleRepository
            .Setup(x => x.AddAsync(It.IsAny<Rule>(), It.IsAny<CancellationToken>()))
            .Callback<Rule, CancellationToken>((r, _) => addedRule = r);

        _mockRuleVersionRepository
            .Setup(x => x.AddAsync(It.IsAny<RuleVersion>(), It.IsAny<CancellationToken>()))
            .Callback<RuleVersion, CancellationToken>((v, _) => addedVersion = v);

        _mockRuleDeploymentRepository
            .Setup(x => x.AddAsync(It.IsAny<RuleDeployment>(), It.IsAny<CancellationToken>()))
            .Callback<RuleDeployment, CancellationToken>((d, _) => addedDeployment = d);

        _mockRuleRepository
            .Setup(x => x.GetAsync(It.IsAny<RuleId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rule);

        // Act
        await _sut.CreateAsync(createRuleRequest, CancellationToken.None);

        // Assert
        addedRule.Should().NotBeNull();
        addedVersion.Should().NotBeNull();
        addedVersion!.VersionNumber.Should().Be(new VersionNumber(1));
        addedVersion.RuleVersionId.Should().Be(addedRule!.CurrentRuleVersionId);
        addedVersion.RuleId.Should().Be(addedRule.RuleId);
        addedVersion.CreatedByUserId.Should().Be(addedRule.CreatedByUserId);
        addedDeployment.Should().NotBeNull();
        addedDeployment!.Strategy.Should().Be(DeploymentStrategy.AllAtOnce);
        addedDeployment.Status.Should().Be(RuleDeploymentStatus.Completed);
        addedDeployment.CurrentPercentage.Should().Be(new RolloutPercentage(100));
        addedDeployment.TargetRuleVersionId.Should().Be(addedVersion.RuleVersionId);
        addedDeployment.PreviousRuleVersionId.Should().BeNull();
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EvaluateAsync_GradualDeploymentActive_RoutesToTargetVersion()
    {
        // Arrange
        var targetRuleVersionId = FakeRuleVersionId.CreateValid(Fixture);
        var rule = FakeRule.CreateValid(Fixture) with
        {
            RuleBody = new RuleBody
            {
                Condition = RuleCondition.EqualTo,
                Field = new RuleField("Key"),
                Value = new RuleValue("Value"),
                And = null,
                Or = null,
                Not = null
            },
            IsEnabled = new IsEnabled(true),
            IsDeleted = new IsDeleted(false),
            HasActiveDeployment = new HasActiveDeployment(true)
        };

        var targetVersion = FakeRuleVersion.CreateValid(Fixture) with
        {
            RuleVersionId = targetRuleVersionId,
            RuleId = rule.RuleId,
            RuleBody = new RuleBody
            {
                Condition = RuleCondition.EqualTo,
                Field = new RuleField("Key"),
                Value = new RuleValue("Value"),
                And = null,
                Or = null,
                Not = null
            },
            Action = RuleAction.Block,
            Severity = RuleSeverity.Critical,
            RiskWeight = new RiskWeight(75)
        };

        var activeDeployment = FakeRuleDeployment.CreateValid(Fixture) with
        {
            RuleId = rule.RuleId,
            Strategy = DeploymentStrategy.Gradual,
            Status = RuleDeploymentStatus.InProgress,
            TargetRuleVersionId = targetRuleVersionId,
            CurrentPercentage = new RolloutPercentage(100)
        };

        var evaluateRuleRequest = FakeEvaluateRuleRequest.CreateValid(Fixture) with
        {
            RuleId = rule.RuleId,
            CreateCase = new CreateCase(false)
        };

        CreateRuleEvaluationRequest? capturedRequest = null;

        _mockRuleRepository
            .Setup(x => x.GetAsync(It.IsAny<RuleId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rule);

        _mockRuleDeploymentRepository
            .Setup(x => x.GetActiveByRuleIdAsync(It.IsAny<RuleId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeDeployment);

        _mockRuleVersionRepository
            .Setup(x => x.GetAsync(It.IsAny<RuleVersionId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetVersion);

        _mockRandomProvider
            .Setup(x => x.Next(100))
            .Returns(0);

        _mockRuleEvaluationService
            .Setup(x => x.CreateAsync(It.IsAny<CreateRuleEvaluationRequest>(), It.IsAny<CancellationToken>()))
            .Callback<CreateRuleEvaluationRequest, CancellationToken>((r, _) => capturedRequest = r)
            .ReturnsAsync(FakeRuleEvaluation.CreateValid(Fixture));

        // Act
        var evaluateRuleResponse = await _sut.EvaluateAsync(evaluateRuleRequest, CancellationToken.None);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.RuleVersionId.Should().Be(targetRuleVersionId);

        evaluateRuleResponse.Policy.Action.Should().Be(RuleAction.Block);
        evaluateRuleResponse.Policy.Severity.Should().Be(RuleSeverity.Critical);
        ((int)evaluateRuleResponse.Policy.RiskWeight).Should().Be(75);
    }

    [Fact]
    public async Task EvaluateAsync_PinsCurrentRuleVersionIdToEvaluation()
    {
        // Arrange
        var ruleVersionId = FakeRuleVersionId.CreateValid(Fixture);
        var rule = FakeRule.CreateValid(Fixture) with
        {
            RuleBody = new RuleBody
            {
                Condition = RuleCondition.EqualTo,
                Field = new RuleField("Key"),
                Value = new RuleValue("Value"),
                And = null,
                Or = null,
                Not = null
            },
            IsEnabled = new IsEnabled(true),
            IsDeleted = new IsDeleted(false),
            CurrentRuleVersionId = ruleVersionId
        };

        var evaluateRuleRequest = FakeEvaluateRuleRequest.CreateValid(Fixture) with
        {
            RuleId = rule.RuleId,
            CreateCase = new CreateCase(false)
        };

        CreateRuleEvaluationRequest? capturedRequest = null;

        _mockRuleRepository
            .Setup(x => x.GetAsync(It.IsAny<RuleId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rule);

        _mockRuleEvaluationService
            .Setup(x => x.CreateAsync(It.IsAny<CreateRuleEvaluationRequest>(), It.IsAny<CancellationToken>()))
            .Callback<CreateRuleEvaluationRequest, CancellationToken>((r, _) => capturedRequest = r)
            .ReturnsAsync(FakeRuleEvaluation.CreateValid(Fixture));

        // Act
        await _sut.EvaluateAsync(evaluateRuleRequest, CancellationToken.None);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.RuleVersionId.Should().Be(ruleVersionId);
    }

    [Fact]
    public async Task EvaluateAsync_HangsTheEvaluationCaseAndResponseOffOneTransaction()
    {
        var transaction = FakeTransaction.CreateValid(Fixture);
        var rule = FakeRule.CreateValid(Fixture) with
        {
            RuleBody = new RuleBody
            {
                Condition = RuleCondition.EqualTo,
                Field = new RuleField("Key"),
                Value = new RuleValue("Value"),
                And = null,
                Or = null,
                Not = null
            },
            IsEnabled = new IsEnabled(true),
            IsDeleted = new IsDeleted(false)
        };

        var evaluateRuleRequest = FakeEvaluateRuleRequest.CreateValid(Fixture) with
        {
            RuleId = rule.RuleId,
            CreateCase = new CreateCase(true)
        };

        CreateTransactionRequest? capturedTransactionRequest = null;
        CreateRuleEvaluationRequest? capturedEvaluationRequest = null;
        CreateCaseRequest? capturedCaseRequest = null;

        _mockRuleRepository
            .Setup(x => x.GetAsync(It.IsAny<RuleId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rule);

        _mockTransactionService
            .Setup(x => x.GetOrCreateAsync(It.IsAny<CreateTransactionRequest>(), It.IsAny<CancellationToken>()))
            .Callback<CreateTransactionRequest, CancellationToken>((r, _) => capturedTransactionRequest = r)
            .ReturnsAsync(transaction);

        _mockRuleEvaluationService
            .Setup(x => x.CreateAsync(It.IsAny<CreateRuleEvaluationRequest>(), It.IsAny<CancellationToken>()))
            .Callback<CreateRuleEvaluationRequest, CancellationToken>((r, _) => capturedEvaluationRequest = r)
            .ReturnsAsync(FakeRuleEvaluation.CreateValid(Fixture));

        _mockCaseService
            .Setup(x => x.CreateAsync(It.IsAny<CreateCaseRequest>(), It.IsAny<CancellationToken>()))
            .Callback<CreateCaseRequest, CancellationToken>((r, _) => capturedCaseRequest = r)
            .ReturnsAsync(FakeCase.CreateValid(Fixture));

        var evaluateRuleResponse = await _sut.EvaluateAsync(evaluateRuleRequest, CancellationToken.None);

        capturedTransactionRequest.Should().NotBeNull();
        capturedTransactionRequest!.OrganizationId.Should().Be(rule.OrganizationId);
        capturedTransactionRequest.ExternalReference.Should().Be(evaluateRuleRequest.ExternalReference);
        capturedTransactionRequest.Payload.ToJsonString().Should().Be(evaluateRuleRequest.Payload.ToJsonString());

        capturedEvaluationRequest.Should().NotBeNull();
        capturedEvaluationRequest!.TransactionId.Should().Be(transaction.TransactionId);

        capturedCaseRequest.Should().NotBeNull();
        capturedCaseRequest!.TransactionId.Should().Be(transaction.TransactionId);

        evaluateRuleResponse.TransactionId.Should().Be(transaction.TransactionId);
        evaluateRuleResponse.ExternalReference.Should().Be(transaction.ExternalReference);
    }

    [Fact]
    public async Task EvaluateAsync_WithoutAnExternalReference_StillCreatesATransaction()
    {
        var transaction = FakeTransaction.CreateValid(Fixture) with
        {
            ExternalReference = null
        };

        var rule = FakeRule.CreateValid(Fixture) with
        {
            RuleBody = new RuleBody
            {
                Condition = RuleCondition.EqualTo,
                Field = new RuleField("Key"),
                Value = new RuleValue("Value"),
                And = null,
                Or = null,
                Not = null
            },
            IsEnabled = new IsEnabled(true),
            IsDeleted = new IsDeleted(false)
        };

        var evaluateRuleRequest = FakeEvaluateRuleRequest.CreateValid(Fixture) with
        {
            RuleId = rule.RuleId,
            ExternalReference = null,
            CreateCase = new CreateCase(false)
        };

        CreateTransactionRequest? capturedTransactionRequest = null;

        _mockRuleRepository
            .Setup(x => x.GetAsync(It.IsAny<RuleId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rule);

        _mockTransactionService
            .Setup(x => x.GetOrCreateAsync(It.IsAny<CreateTransactionRequest>(), It.IsAny<CancellationToken>()))
            .Callback<CreateTransactionRequest, CancellationToken>((r, _) => capturedTransactionRequest = r)
            .ReturnsAsync(transaction);

        var evaluateRuleResponse = await _sut.EvaluateAsync(evaluateRuleRequest, CancellationToken.None);

        capturedTransactionRequest.Should().NotBeNull();
        capturedTransactionRequest!.ExternalReference.Should().BeNull();

        evaluateRuleResponse.TransactionId.Should().Be(transaction.TransactionId);
        evaluateRuleResponse.ExternalReference.Should().BeNull();
    }

    [Fact]
    public async Task EvaluateAsync_RuleDoesNotMatch_DoesNotCreateCase()
    {
        // Arrange
        var rule = FakeRule.CreateValid(Fixture) with
        {
            RuleBody = new RuleBody
            {
                Condition = RuleCondition.EqualTo,
                Field = new RuleField("Key"),
                Value = new RuleValue("Not Value"),
                And = null,
                Or = null,
                Not = null
            },
            IsEnabled = new IsEnabled(true),
            IsDeleted = new IsDeleted(false)
        };

        var evaluateRuleRequest = FakeEvaluateRuleRequest.CreateValid(Fixture) with
        {
            RuleId = rule.RuleId,
            CreateCase = new CreateCase(true)
        };

        _mockRuleRepository
            .Setup(x => x.GetAsync(It.IsAny<RuleId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rule);

        // Act
        var evaluateRuleResponse = await _sut.EvaluateAsync(evaluateRuleRequest, CancellationToken.None);

        // Assert
        evaluateRuleResponse.RuleId.Should().Be(evaluateRuleRequest.RuleId);
        evaluateRuleResponse.Matched.Should().Be(new Matched(false));
        evaluateRuleResponse.CaseId.Should().BeNull();
        evaluateRuleResponse.Case.Should().BeNull();

        _mockRuleRepository.Verify(x => x.GetAsync(It.IsAny<RuleId>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUsageService.Verify(x => x.RecordBillableAsync(It.IsAny<CreateUsageRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockCaseService.Verify(x => x.CreateAsync(It.IsAny<CreateCaseRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EvaluateAsync_RuleOpensCase_RecordsTheDecisionAgainstTheSameTransactionAndCase()
    {
        // Arrange
        var transaction = FakeTransaction.CreateValid(Fixture);
        var @case = FakeCase.CreateValid(Fixture);
        var rule = FakeRule.CreateValid(Fixture) with
        {
            RuleBody = new RuleBody
            {
                Condition = RuleCondition.EqualTo,
                Field = new RuleField("Key"),
                Value = new RuleValue("Value"),
                And = null,
                Or = null,
                Not = null
            },
            Action = RuleAction.Review,
            IsEnabled = new IsEnabled(true),
            IsDeleted = new IsDeleted(false)
        };

        var evaluateRuleRequest = FakeEvaluateRuleRequest.CreateValid(Fixture) with
        {
            RuleId = rule.RuleId,
            CreateCase = new CreateCase(true)
        };

        RecordDecisionRequest? capturedDecisionRequest = null;

        _mockRuleRepository
            .Setup(x => x.GetAsync(It.IsAny<RuleId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rule);

        _mockTransactionService
            .Setup(x => x.GetOrCreateAsync(It.IsAny<CreateTransactionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);

        _mockRuleEvaluationService
            .Setup(x => x.CreateAsync(It.IsAny<CreateRuleEvaluationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakeRuleEvaluation.CreateValid(Fixture));

        _mockCaseService
            .Setup(x => x.CreateAsync(It.IsAny<CreateCaseRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(@case);

        _mockDecisionService
            .Setup(x => x.RecordAsync(It.IsAny<RecordDecisionRequest>(), It.IsAny<CancellationToken>()))
            .Callback<RecordDecisionRequest, CancellationToken>((r, _) => capturedDecisionRequest = r)
            .ReturnsAsync(FakeDecision.CreateValid(Fixture));

        // Act
        var evaluateRuleResponse = await _sut.EvaluateAsync(evaluateRuleRequest, CancellationToken.None);

        // Assert
        capturedDecisionRequest.Should().NotBeNull();
        capturedDecisionRequest!.TransactionId.Should().Be(transaction.TransactionId);
        capturedDecisionRequest.OrganizationId.Should().Be(rule.OrganizationId);
        capturedDecisionRequest.CaseId.Should().Be(@case.CaseId);
        capturedDecisionRequest.AnyMatched.Should().Be(new Matched(true));
        capturedDecisionRequest.Outcome.Action.Should().Be(RuleAction.Review);

        evaluateRuleResponse.Decision.Should().NotBeNull();
    }

    [Fact]
    public async Task EvaluateAsync_RuleDoesNotMatch_StillRecordsAReleasedDecision()
    {
        // Arrange
        var transaction = FakeTransaction.CreateValid(Fixture);
        var rule = FakeRule.CreateValid(Fixture) with
        {
            RuleBody = new RuleBody
            {
                Condition = RuleCondition.EqualTo,
                Field = new RuleField("Key"),
                Value = new RuleValue("Not Value"),
                And = null,
                Or = null,
                Not = null
            },
            IsEnabled = new IsEnabled(true),
            IsDeleted = new IsDeleted(false)
        };

        var evaluateRuleRequest = FakeEvaluateRuleRequest.CreateValid(Fixture) with
        {
            RuleId = rule.RuleId,
            CreateCase = new CreateCase(true)
        };

        RecordDecisionRequest? capturedDecisionRequest = null;

        _mockRuleRepository
            .Setup(x => x.GetAsync(It.IsAny<RuleId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rule);

        _mockTransactionService
            .Setup(x => x.GetOrCreateAsync(It.IsAny<CreateTransactionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);

        _mockDecisionService
            .Setup(x => x.RecordAsync(It.IsAny<RecordDecisionRequest>(), It.IsAny<CancellationToken>()))
            .Callback<RecordDecisionRequest, CancellationToken>((r, _) => capturedDecisionRequest = r)
            .ReturnsAsync(FakeDecision.CreateValid(Fixture));

        // Act
        await _sut.EvaluateAsync(evaluateRuleRequest, CancellationToken.None);

        // Assert
        capturedDecisionRequest.Should().NotBeNull();
        capturedDecisionRequest!.CaseId.Should().BeNull();
        capturedDecisionRequest.AnyMatched.Should().Be(new Matched(false));
        capturedDecisionRequest.Outcome.Action.Should().Be(RuleAction.Allow);
    }

    [Fact]
    public async Task EvaluateAsync_RuleMatches_DoesCreateCase()
    {
        // Arrange
        var @case = FakeCase.CreateValid(Fixture);
        var ruleEvaluation = FakeRuleEvaluation.CreateValid(Fixture);
        var rule = FakeRule.CreateValid(Fixture) with
        {
            RuleBody = new RuleBody
            {
                Condition = RuleCondition.EqualTo,
                Field = new RuleField("Key"),
                Value = new RuleValue("Value"),
                And = null,
                Or = null,
                Not = null
            },
            IsEnabled = new IsEnabled(true),
            IsDeleted = new IsDeleted(false)
        };

        var evaluateRuleRequest = FakeEvaluateRuleRequest.CreateValid(Fixture) with
        {
            RuleId = rule.RuleId,
            CreateCase = new CreateCase(true)
        };

        _mockRuleRepository
            .Setup(x => x.GetAsync(It.IsAny<RuleId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rule);

        _mockRuleEvaluationService
            .Setup(x => x.CreateAsync(It.IsAny<CreateRuleEvaluationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ruleEvaluation);

        _mockCaseService
            .Setup(x => x.CreateAsync(It.IsAny<CreateCaseRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(@case);

        // Act
        var evaluateRuleResponse = await _sut.EvaluateAsync(evaluateRuleRequest, CancellationToken.None);

        // Assert
        evaluateRuleResponse.RuleId.Should().Be(evaluateRuleRequest.RuleId);
        evaluateRuleResponse.Matched.Should().Be(new Matched(true));
        evaluateRuleResponse.CaseId.Should().Be(@case.CaseId);
        evaluateRuleResponse.Case.Should().BeEquivalentTo(@case);

        _mockRuleRepository.Verify(x => x.GetAsync(It.IsAny<RuleId>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUsageService.Verify(x => x.RecordBillableAsync(It.IsAny<CreateUsageRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockCaseService.Verify(x => x.CreateAsync(It.IsAny<CreateCaseRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EvaluateAsync_TransactionAlreadyDecided_DoesNotCreateCase()
    {
        // Arrange
        var @case = FakeCase.CreateValid(Fixture);
        var ruleEvaluation = FakeRuleEvaluation.CreateValid(Fixture);
        var rule = FakeRule.CreateValid(Fixture) with
        {
            RuleBody = new RuleBody
            {
                Condition = RuleCondition.EqualTo,
                Field = new RuleField("Key"),
                Value = new RuleValue("Value"),
                And = null,
                Or = null,
                Not = null
            },
            IsEnabled = new IsEnabled(true),
            IsDeleted = new IsDeleted(false)
        };

        var evaluateRuleRequest = FakeEvaluateRuleRequest.CreateValid(Fixture) with
        {
            RuleId = rule.RuleId,
            CreateCase = new CreateCase(true)
        };

        _mockRuleRepository
            .Setup(x => x.GetAsync(It.IsAny<RuleId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rule);

        _mockRuleEvaluationService
            .Setup(x => x.CreateAsync(It.IsAny<CreateRuleEvaluationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ruleEvaluation);

        _mockCaseService
            .Setup(x => x.CreateAsync(It.IsAny<CreateCaseRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(@case);

        _mockDecisionService
            .Setup(x => x.FindByTransactionAsync(It.IsAny<TransactionId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakeDecision.CreateValid(Fixture));

        // Act
        var evaluateRuleResponse = await _sut.EvaluateAsync(evaluateRuleRequest, CancellationToken.None);

        // Assert
        evaluateRuleResponse.Matched.Should().Be(new Matched(true));
        evaluateRuleResponse.CaseId.Should().BeNull();
        evaluateRuleResponse.Case.Should().BeNull();

        _mockCaseService.Verify(x => x.CreateAsync(It.IsAny<CreateCaseRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EvaluateAsync_RuleMatches_CreateCaseSetToFalse_DoesNotCreateCase()
    {
        // Arrange
        var rule = FakeRule.CreateValid(Fixture) with
        {
            RuleBody = new RuleBody
            {
                Condition = RuleCondition.EqualTo,
                Field = new RuleField("Key"),
                Value = new RuleValue("Value"),
                And = null,
                Or = null,
                Not = null
            },
            IsEnabled = new IsEnabled(true),
            IsDeleted = new IsDeleted(false)
        };

        var evaluateRuleRequest = FakeEvaluateRuleRequest.CreateValid(Fixture) with
        {
            RuleId = rule.RuleId,
            CreateCase = new CreateCase(false)
        };

        _mockRuleRepository
            .Setup(x => x.GetAsync(It.IsAny<RuleId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rule);

        // Act
        var evaluateRuleResponse = await _sut.EvaluateAsync(evaluateRuleRequest, CancellationToken.None);

        // Assert
        evaluateRuleResponse.RuleId.Should().Be(evaluateRuleRequest.RuleId);
        evaluateRuleResponse.Matched.Should().Be(new Matched(true));
        evaluateRuleResponse.CaseId.Should().BeNull();
        evaluateRuleResponse.Case.Should().BeNull();

        _mockRuleRepository.Verify(x => x.GetAsync(It.IsAny<RuleId>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUsageService.Verify(x => x.RecordBillableAsync(It.IsAny<CreateUsageRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockCaseService.Verify(x => x.CreateAsync(It.IsAny<CreateCaseRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(RuleAction.Review, true)]
    [InlineData(RuleAction.Block, true)]
    [InlineData(RuleAction.Flag, false)]
    [InlineData(RuleAction.Allow, false)]
    public async Task EvaluateAsync_RuleFires_CreatesCaseOnlyForRestrictiveActions(RuleAction action, bool expectsCase)
    {
        // Arrange
        var rule = FakeRule.CreateValid(Fixture) with
        {
            RuleBody = new RuleBody
            {
                Condition = RuleCondition.EqualTo,
                Field = new RuleField("Key"),
                Value = new RuleValue("Value"),
                And = null,
                Or = null,
                Not = null
            },
            Action = action,
            RiskWeight = new RiskWeight(0),
            IsEnabled = new IsEnabled(true),
            IsDeleted = new IsDeleted(false)
        };

        var evaluateRuleRequest = FakeEvaluateRuleRequest.CreateValid(Fixture) with
        {
            RuleId = rule.RuleId,
            CreateCase = new CreateCase(true)
        };

        _mockRuleRepository
            .Setup(x => x.GetAsync(It.IsAny<RuleId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rule);

        _mockRuleEvaluationService
            .Setup(x => x.CreateAsync(It.IsAny<CreateRuleEvaluationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakeRuleEvaluation.CreateValid(Fixture));

        _mockCaseService
            .Setup(x => x.CreateAsync(It.IsAny<CreateCaseRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakeCase.CreateValid(Fixture));

        // Act
        var evaluateRuleResponse = await _sut.EvaluateAsync(evaluateRuleRequest, CancellationToken.None);

        // Assert
        evaluateRuleResponse.Matched.Should().Be(new Matched(true));
        evaluateRuleResponse.Outcome.Action.Should().Be(action);

        _mockCaseService.Verify(
            x => x.CreateAsync(It.IsAny<CreateCaseRequest>(), It.IsAny<CancellationToken>()),
            expectsCase ? Times.Once() : Times.Never());
    }

    [Fact]
    public async Task EvaluateAsync_FlagRuleClearsRuleSetThreshold_EscalatesToReviewAndCreatesCase()
    {
        // Arrange
        var rule = FakeRule.CreateValid(Fixture) with
        {
            RuleBody = new RuleBody
            {
                Condition = RuleCondition.EqualTo,
                Field = new RuleField("Key"),
                Value = new RuleValue("Value"),
                And = null,
                Or = null,
                Not = null
            },
            Action = RuleAction.Flag,
            RiskWeight = new RiskWeight(30),
            IsEnabled = new IsEnabled(true),
            IsDeleted = new IsDeleted(false)
        };

        var evaluateRuleRequest = FakeEvaluateRuleRequest.CreateValid(Fixture) with
        {
            RuleId = rule.RuleId,
            CreateCase = new CreateCase(true)
        };

        _mockRuleRepository
            .Setup(x => x.GetAsync(It.IsAny<RuleId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rule);

        _mockRuleSetRepository
            .Setup(x => x.GetReviewThresholdAsync(It.IsAny<RuleSetId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReviewThreshold(30));

        _mockRuleEvaluationService
            .Setup(x => x.CreateAsync(It.IsAny<CreateRuleEvaluationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakeRuleEvaluation.CreateValid(Fixture));

        _mockCaseService
            .Setup(x => x.CreateAsync(It.IsAny<CreateCaseRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakeCase.CreateValid(Fixture));

        // Act
        var evaluateRuleResponse = await _sut.EvaluateAsync(evaluateRuleRequest, CancellationToken.None);

        // Assert
        evaluateRuleResponse.Policy.Action.Should().Be(RuleAction.Flag);
        evaluateRuleResponse.Outcome.Action.Should().Be(RuleAction.Review);
        ((int)evaluateRuleResponse.Outcome.RiskScore).Should().Be(30);

        _mockCaseService.Verify(x => x.CreateAsync(It.IsAny<CreateCaseRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(RuleMatchPolarity.MatchesWhenTrue, "Value", true)]
    [InlineData(RuleMatchPolarity.MatchesWhenTrue, "Not Value", false)]
    [InlineData(RuleMatchPolarity.MatchesWhenFalse, "Value", false)]
    [InlineData(RuleMatchPolarity.MatchesWhenFalse, "Not Value", true)]
    public async Task EvaluateAsync_ReadsMatchPolarityFromTheRule(RuleMatchPolarity matchPolarity, string ruleValue, bool expectedMatched)
    {
        // Arrange
        var rule = FakeRule.CreateValid(Fixture) with
        {
            RuleBody = new RuleBody
            {
                Condition = RuleCondition.EqualTo,
                Field = new RuleField("Key"),
                Value = new RuleValue(ruleValue),
                And = null,
                Or = null,
                Not = null
            },
            MatchPolarity = matchPolarity,
            Action = RuleAction.Review,
            RiskWeight = new RiskWeight(0),
            IsEnabled = new IsEnabled(true),
            IsDeleted = new IsDeleted(false)
        };

        var evaluateRuleRequest = FakeEvaluateRuleRequest.CreateValid(Fixture) with
        {
            RuleId = rule.RuleId,
            CreateCase = new CreateCase(true)
        };

        _mockRuleRepository
            .Setup(x => x.GetAsync(It.IsAny<RuleId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rule);

        _mockRuleEvaluationService
            .Setup(x => x.CreateAsync(It.IsAny<CreateRuleEvaluationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakeRuleEvaluation.CreateValid(Fixture));

        _mockCaseService
            .Setup(x => x.CreateAsync(It.IsAny<CreateCaseRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakeCase.CreateValid(Fixture));

        // Act
        var evaluateRuleResponse = await _sut.EvaluateAsync(evaluateRuleRequest, CancellationToken.None);

        // Assert
        evaluateRuleResponse.Matched.Should().Be(new Matched(expectedMatched));
        evaluateRuleResponse.Outcome.Action.Should().Be(expectedMatched ? RuleAction.Review : RuleAction.Allow);

        _mockCaseService.Verify(
            x => x.CreateAsync(It.IsAny<CreateCaseRequest>(), It.IsAny<CancellationToken>()),
            expectedMatched ? Times.Once() : Times.Never());
    }

    [Theory]
    [InlineData("Value", RuleMatchPolarity.MatchesWhenTrue, true)]
    [InlineData("Not Value", RuleMatchPolarity.MatchesWhenTrue, false)]
    [InlineData("Value", RuleMatchPolarity.MatchesWhenFalse, false)]
    [InlineData("Not Value", RuleMatchPolarity.MatchesWhenFalse, true)]
    public async Task TestAsync_ReturnsMatchedAndTheResultingOutcome(string ruleValue, RuleMatchPolarity matchPolarity, bool expectedMatched)
    {
        // Arrange
        var testRuleRequest = new TestRuleRequest
        {
            RuleBody = new RuleBody
            {
                Condition = RuleCondition.EqualTo,
                Field = new RuleField("Key"),
                Value = new RuleValue(ruleValue),
                And = null,
                Or = null,
                Not = null
            },
            Payload = FakeJsonObject.CreateValid(Fixture),
            MatchPolarity = matchPolarity,
            Action = RuleAction.Block,
            Severity = RuleSeverity.Critical,
            RiskWeight = new RiskWeight(40),
            OrganizationId = FakeOrganizationId.CreateValid(Fixture)
        };

        // Act
        var testRuleResponse = await _sut.TestAsync(testRuleRequest, CancellationToken.None);

        // Assert
        testRuleResponse.Matched.Should().Be(new Matched(expectedMatched));
        testRuleResponse.Policy.Action.Should().Be(RuleAction.Block);
        testRuleResponse.Outcome.Action.Should().Be(expectedMatched ? RuleAction.Block : RuleAction.Allow);
        ((int)testRuleResponse.Outcome.RiskScore).Should().Be(expectedMatched ? 40 : 0);

        _mockUsageService.Verify(x => x.RecordBillableAsync(It.IsAny<CreateUsageRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EvaluateAsync_RuleFires_CaseTakesTheRuleSeverityAndTheEvaluationCarriesTheConditions()
    {
        // Arrange
        var rule = FakeRule.CreateValid(Fixture) with
        {
            RuleBody = new RuleBody
            {
                Condition = RuleCondition.EqualTo,
                Field = new RuleField("Key"),
                Value = new RuleValue("Value"),
                And = null,
                Or = null,
                Not = null
            },
            MatchPolarity = RuleMatchPolarity.MatchesWhenTrue,
            Action = RuleAction.Review,
            Severity = RuleSeverity.High,
            RiskWeight = new RiskWeight(0),
            HasActiveDeployment = new HasActiveDeployment(false),
            IsEnabled = new IsEnabled(true),
            IsDeleted = new IsDeleted(false)
        };

        var evaluateRuleRequest = FakeEvaluateRuleRequest.CreateValid(Fixture) with
        {
            RuleId = rule.RuleId,
            CreateCase = new CreateCase(true)
        };

        CreateRuleEvaluationRequest? capturedEvaluationRequest = null;
        CreateCaseRequest? capturedCaseRequest = null;

        _mockRuleRepository
            .Setup(x => x.GetAsync(It.IsAny<RuleId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rule);

        _mockRuleEvaluationService
            .Setup(x => x.CreateAsync(It.IsAny<CreateRuleEvaluationRequest>(), It.IsAny<CancellationToken>()))
            .Callback<CreateRuleEvaluationRequest, CancellationToken>((r, _) => capturedEvaluationRequest = r)
            .ReturnsAsync(FakeRuleEvaluation.CreateValid(Fixture));

        _mockCaseService
            .Setup(x => x.CreateAsync(It.IsAny<CreateCaseRequest>(), It.IsAny<CancellationToken>()))
            .Callback<CreateCaseRequest, CancellationToken>((r, _) => capturedCaseRequest = r)
            .ReturnsAsync(FakeCase.CreateValid(Fixture));

        // Act
        await _sut.EvaluateAsync(evaluateRuleRequest, CancellationToken.None);

        // Assert
        capturedCaseRequest.Should().NotBeNull();
        capturedCaseRequest!.Severity.Should().Be(RuleSeverity.High);

        capturedEvaluationRequest.Should().NotBeNull();
        capturedEvaluationRequest!.ConditionMatches.Should().ContainSingle();
        capturedEvaluationRequest.ConditionMatches![0].Field.Should().Be("Key");
        capturedEvaluationRequest.ConditionMatches[0].Expected!.ToJsonString().Should().Be("\"Value\"");
        capturedEvaluationRequest.ConditionMatches[0].Actual!.ToJsonString().Should().Be("\"Value\"");
        capturedEvaluationRequest.ConditionMatches[0].Matched.Should().BeTrue();
    }
}