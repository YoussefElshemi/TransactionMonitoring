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
using UnitTests.TestHelpers.FakeObjects.Core.Models.RuleEvaluations;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Rules;
using UnitTests.TestHelpers.FakeObjects.Core.Models.RuleSets;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Transactions;

namespace UnitTests.Tests.Core.Services;

public class RuleSetServiceTests : BaseTestClass
{
    private readonly Mock<ICaseService> _mockCaseService = new();
    private readonly Mock<IAlertService> _mockAlertService = new();
    private readonly Mock<IDecisionService> _mockDecisionService = new();
    private readonly Mock<IRuleEvaluationService> _mockRuleEvaluationService = new();
    private readonly Mock<IRuleSetRepository> _mockRuleSetRepository = new();
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();
    private readonly Mock<ISandboxCreditService> _mockSandboxCreditService = new();
    private readonly Mock<IUsageService> _mockUsageService = new();
    private readonly Mock<ITransactionService> _mockTransactionService = new();
    private readonly Mock<ITransactionSchemaService> _mockTransactionSchemaService = new();
    private readonly Mock<IPayloadProtectionService> _mockPayloadProtectionService = new();
    private readonly Mock<IPartyService> _mockPartyService = new();
    private readonly Mock<IVelocityService> _mockVelocityService = new();
    private readonly RuleSetService _sut;

    public RuleSetServiceTests()
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
            .Setup(x => x.RecordAndReadAsync(It.IsAny<RecordVelocityObservationsRequest>(), It.IsAny<IReadOnlyList<RuleBodyModel>>(), It.IsAny<EvaluatedAt>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(VelocityFacts.Empty);

        _mockPayloadProtectionService
            .Setup(x => x.GetOrCreateDataKeyAsync(It.IsAny<OrganizationId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(DataKeyWrapper.Create());

        _sut = new RuleSetService(
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
            _mockVelocityService.Object,
            _mockUnitOfWork.Object,
            appConfig,
            MockTimeProvider,
            _mockSandboxCreditService.Object,
            TestModeContext);

        _mockTransactionService
            .Setup(x => x.GetOrCreateAsync(It.IsAny<CreateTransactionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => FakeTransaction.CreateValid(Fixture));

        _mockRuleEvaluationService
            .Setup(x => x.CreateManyAsync(It.IsAny<List<CreateRuleEvaluationRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => []);

        _mockUnitOfWork
            .Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task<(Case? Case, Decision Decision)>>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<Task<(Case? Case, Decision Decision)>> operation, CancellationToken _) => operation());
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

        var ruleSet = FakeRuleSet.CreateValid(Fixture) with
        {
            RuleSetId = rule.RuleSetId,
            Rules = [rule]
        };

        var evaluateRuleSetRequest = FakeEvaluateRuleSetRequest.CreateValid(Fixture) with
        {
            RuleSetId = ruleSet.RuleSetId,
            CreateCase = new CreateCase(true)
        };

        _mockRuleSetRepository
            .Setup(x => x.GetAsync(It.IsAny<RuleSetId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ruleSet);

        // Act
        var evaluateRuleSetResponse = await _sut.EvaluateAsync(evaluateRuleSetRequest, CancellationToken.None);

        // Assert
        evaluateRuleSetResponse.RuleSetId.Should().Be(evaluateRuleSetRequest.RuleSetId);
        evaluateRuleSetResponse.Results.Should().HaveCount(1);
        evaluateRuleSetResponse.Results.First().RuleId.Should().Be(rule.RuleId);
        evaluateRuleSetResponse.Results.First().Matched.Should().Be(new Matched(false));
        evaluateRuleSetResponse.CaseId.Should().BeNull();
        evaluateRuleSetResponse.Case.Should().BeNull();

        _mockRuleSetRepository.Verify(x => x.GetAsync(It.IsAny<RuleSetId>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUsageService.Verify(x => x.RecordBillableAsync(It.IsAny<CreateUsageRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockCaseService.Verify(x => x.CreateAsync(It.IsAny<CreateCaseRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EvaluateAsync_RuleMatches_DoesCreateCase()
    {
        // Arrange
        var @case = FakeCase.CreateValid(Fixture);
        var ruleEvaluation = FakeRuleEvaluation.CreateValid(Fixture) with { Matched = new Matched(true) };
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

        var ruleSet = FakeRuleSet.CreateValid(Fixture) with
        {
            RuleSetId = rule.RuleSetId,
            Rules = [rule]
        };

        var evaluateRuleSetRequest = FakeEvaluateRuleSetRequest.CreateValid(Fixture) with
        {
            RuleSetId = ruleSet.RuleSetId,
            CreateCase = new CreateCase(true)
        };

        _mockRuleSetRepository
            .Setup(x => x.GetAsync(It.IsAny<RuleSetId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ruleSet);

        _mockRuleEvaluationService
            .Setup(x => x.CreateManyAsync(It.IsAny<List<CreateRuleEvaluationRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([ruleEvaluation]);

        _mockCaseService
            .Setup(x => x.CreateAsync(It.IsAny<CreateCaseRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(@case);


        // Act
        var evaluateRuleSetResponse = await _sut.EvaluateAsync(evaluateRuleSetRequest, CancellationToken.None);

        // Assert
        evaluateRuleSetResponse.RuleSetId.Should().Be(evaluateRuleSetRequest.RuleSetId);
        evaluateRuleSetResponse.Results.Should().HaveCount(1);
        evaluateRuleSetResponse.Results.First().RuleId.Should().Be(rule.RuleId);
        evaluateRuleSetResponse.Results.First().Matched.Should().Be(new Matched(true));
        evaluateRuleSetResponse.CaseId.Should().Be(@case.CaseId);
        evaluateRuleSetResponse.Case.Should().Be(@case);

        _mockRuleSetRepository.Verify(x => x.GetAsync(It.IsAny<RuleSetId>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUsageService.Verify(x => x.RecordBillableAsync(It.IsAny<CreateUsageRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockCaseService.Verify(x => x.CreateAsync(It.IsAny<CreateCaseRequest>(), It.IsAny<CancellationToken>()), Times.Once);
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

        var ruleSet = FakeRuleSet.CreateValid(Fixture) with
        {
            RuleSetId = rule.RuleSetId,
            Rules = [rule]
        };

        var evaluateRuleSetRequest = FakeEvaluateRuleSetRequest.CreateValid(Fixture) with
        {
            RuleSetId = ruleSet.RuleSetId,
            CreateCase = new CreateCase(false)
        };

        _mockRuleSetRepository
            .Setup(x => x.GetAsync(It.IsAny<RuleSetId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ruleSet);

        // Act
        var evaluateRuleSetResponse = await _sut.EvaluateAsync(evaluateRuleSetRequest, CancellationToken.None);

        // Assert
        evaluateRuleSetResponse.RuleSetId.Should().Be(evaluateRuleSetRequest.RuleSetId);
        evaluateRuleSetResponse.Results.Should().HaveCount(1);
        evaluateRuleSetResponse.Results.First().RuleId.Should().Be(rule.RuleId);
        evaluateRuleSetResponse.Results.First().Matched.Should().Be(new Matched(true));
        evaluateRuleSetResponse.CaseId.Should().BeNull();
        evaluateRuleSetResponse.Case.Should().BeNull();

        _mockRuleSetRepository.Verify(x => x.GetAsync(It.IsAny<RuleSetId>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUsageService.Verify(x => x.RecordBillableAsync(It.IsAny<CreateUsageRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockCaseService.Verify(x => x.CreateAsync(It.IsAny<CreateCaseRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EvaluateAsync_HangsEveryEvaluationInTheSetOffOneTransaction()
    {
        var transaction = FakeTransaction.CreateValid(Fixture);
        var rules = Enumerable.Range(0, 3).Select(_ => FakeRule.CreateValid(Fixture) with
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
        }).ToList();

        var ruleSet = FakeRuleSet.CreateValid(Fixture) with
        {
            Rules = rules
        };

        var evaluateRuleSetRequest = FakeEvaluateRuleSetRequest.CreateValid(Fixture) with
        {
            RuleSetId = ruleSet.RuleSetId,
            CreateCase = new CreateCase(true)
        };

        List<CreateRuleEvaluationRequest>? capturedEvaluationRequests = null;
        CreateCaseRequest? capturedCaseRequest = null;

        _mockRuleSetRepository
            .Setup(x => x.GetAsync(It.IsAny<RuleSetId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ruleSet);

        _mockTransactionService
            .Setup(x => x.GetOrCreateAsync(It.IsAny<CreateTransactionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);

        _mockRuleEvaluationService
            .Setup(x => x.CreateManyAsync(It.IsAny<List<CreateRuleEvaluationRequest>>(), It.IsAny<CancellationToken>()))
            .Callback<List<CreateRuleEvaluationRequest>, CancellationToken>((r, _) => capturedEvaluationRequests = r)
            .ReturnsAsync(rules.ConvertAll(_ => FakeRuleEvaluation.CreateValid(Fixture) with { Matched = new Matched(true) }));

        _mockCaseService
            .Setup(x => x.CreateAsync(It.IsAny<CreateCaseRequest>(), It.IsAny<CancellationToken>()))
            .Callback<CreateCaseRequest, CancellationToken>((r, _) => capturedCaseRequest = r)
            .ReturnsAsync(FakeCase.CreateValid(Fixture));

        var evaluateRuleSetResponse = await _sut.EvaluateAsync(evaluateRuleSetRequest, CancellationToken.None);

        capturedEvaluationRequests.Should().HaveCount(3);
        capturedEvaluationRequests.Should().OnlyContain(x => x.TransactionId == transaction.TransactionId);

        capturedCaseRequest.Should().NotBeNull();
        capturedCaseRequest!.TransactionId.Should().Be(transaction.TransactionId);

        evaluateRuleSetResponse.TransactionId.Should().Be(transaction.TransactionId);
        evaluateRuleSetResponse.ExternalReference.Should().Be(transaction.ExternalReference);
        evaluateRuleSetResponse.Results.Should().OnlyContain(x => x.TransactionId == transaction.TransactionId);

        _mockTransactionService.Verify(
            x => x.GetOrCreateAsync(It.IsAny<CreateTransactionRequest>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task EvaluateAllAsync_HangsEveryEvaluationAcrossSetsOffOneTransaction()
    {
        var transaction = FakeTransaction.CreateValid(Fixture);
        var ruleSets = Enumerable.Range(0, 2).Select(_ => FakeRuleSet.CreateValid(Fixture) with
        {
            IsEnabled = new IsEnabled(true),
            IsDeleted = new IsDeleted(false),
            Rules =
            [
                FakeRule.CreateValid(Fixture) with
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
                }
            ]
        }).ToList();

        var evaluateRuleSetsRequest = FakeEvaluateRuleSetsRequest.CreateValid(Fixture) with
        {
            CreateCase = new CreateCase(true)
        };

        List<CreateRuleEvaluationRequest>? capturedEvaluationRequests = null;
        CreateCaseRequest? capturedCaseRequest = null;

        _mockRuleSetRepository
            .Setup(x => x.GetAllAsync(It.IsAny<OrganizationId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ruleSets);

        _mockTransactionService
            .Setup(x => x.GetOrCreateAsync(It.IsAny<CreateTransactionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);

        _mockRuleEvaluationService
            .Setup(x => x.CreateManyAsync(It.IsAny<List<CreateRuleEvaluationRequest>>(), It.IsAny<CancellationToken>()))
            .Callback<List<CreateRuleEvaluationRequest>, CancellationToken>((r, _) => capturedEvaluationRequests = r)
            .ReturnsAsync(ruleSets.ConvertAll(_ => FakeRuleEvaluation.CreateValid(Fixture) with { Matched = new Matched(true) }));

        _mockCaseService
            .Setup(x => x.CreateAsync(It.IsAny<CreateCaseRequest>(), It.IsAny<CancellationToken>()))
            .Callback<CreateCaseRequest, CancellationToken>((r, _) => capturedCaseRequest = r)
            .ReturnsAsync(FakeCase.CreateValid(Fixture));

        var evaluateAllRulesResponse = await _sut.EvaluateAllAsync(evaluateRuleSetsRequest, CancellationToken.None);

        capturedEvaluationRequests.Should().HaveCount(2);
        capturedEvaluationRequests.Should().OnlyContain(x => x.TransactionId == transaction.TransactionId);

        capturedCaseRequest.Should().NotBeNull();
        capturedCaseRequest!.TransactionId.Should().Be(transaction.TransactionId);

        evaluateAllRulesResponse.TransactionId.Should().Be(transaction.TransactionId);
        evaluateAllRulesResponse.ExternalReference.Should().Be(transaction.ExternalReference);
        evaluateAllRulesResponse.Results.Should().OnlyContain(x => x.TransactionId == transaction.TransactionId);

        _mockTransactionService.Verify(
            x => x.GetOrCreateAsync(It.IsAny<CreateTransactionRequest>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task EvaluateAsync_RulesDisagree_MostRestrictiveActionWins()
    {
        // Arrange
        var flagRule = FiringRule(RuleAction.Flag, RuleSeverity.Low);
        var blockRule = FiringRule(RuleAction.Block, RuleSeverity.Critical) with { RuleSetId = flagRule.RuleSetId };

        var ruleSet = FakeRuleSet.CreateValid(Fixture) with
        {
            RuleSetId = flagRule.RuleSetId,
            ReviewThreshold = new ReviewThreshold(1000),
            Rules = [flagRule, blockRule]
        };

        var evaluateRuleSetRequest = FakeEvaluateRuleSetRequest.CreateValid(Fixture) with
        {
            RuleSetId = ruleSet.RuleSetId,
            CreateCase = new CreateCase(true)
        };

        _mockRuleSetRepository
            .Setup(x => x.GetAsync(It.IsAny<RuleSetId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ruleSet);

        _mockRuleEvaluationService
            .Setup(x => x.CreateManyAsync(It.IsAny<List<CreateRuleEvaluationRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([FakeRuleEvaluation.CreateValid(Fixture) with { Matched = new Matched(true) }]);

        _mockCaseService
            .Setup(x => x.CreateAsync(It.IsAny<CreateCaseRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakeCase.CreateValid(Fixture));

        // Act
        var evaluateRuleSetResponse = await _sut.EvaluateAsync(evaluateRuleSetRequest, CancellationToken.None);

        // Assert
        evaluateRuleSetResponse.Outcome.Action.Should().Be(RuleAction.Block);
        evaluateRuleSetResponse.Outcome.Severity.Should().Be(RuleSeverity.Critical);

        _mockCaseService.Verify(x => x.CreateAsync(It.IsAny<CreateCaseRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EvaluateAsync_SeveralFlagsClearRuleSetThreshold_EscalatesToReview()
    {
        // Arrange
        var first = FiringRule(RuleAction.Flag, RuleSeverity.Low, riskWeight: 10);
        var second = FiringRule(RuleAction.Flag, RuleSeverity.Low, riskWeight: 10) with { RuleSetId = first.RuleSetId };

        var ruleSet = FakeRuleSet.CreateValid(Fixture) with
        {
            RuleSetId = first.RuleSetId,
            ReviewThreshold = new ReviewThreshold(20),
            Rules = [first, second]
        };

        var evaluateRuleSetRequest = FakeEvaluateRuleSetRequest.CreateValid(Fixture) with
        {
            RuleSetId = ruleSet.RuleSetId,
            CreateCase = new CreateCase(true)
        };

        _mockRuleSetRepository
            .Setup(x => x.GetAsync(It.IsAny<RuleSetId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ruleSet);

        _mockRuleEvaluationService
            .Setup(x => x.CreateManyAsync(It.IsAny<List<CreateRuleEvaluationRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([FakeRuleEvaluation.CreateValid(Fixture) with { Matched = new Matched(true) }]);

        _mockCaseService
            .Setup(x => x.CreateAsync(It.IsAny<CreateCaseRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakeCase.CreateValid(Fixture));

        // Act
        var evaluateRuleSetResponse = await _sut.EvaluateAsync(evaluateRuleSetRequest, CancellationToken.None);

        // Assert
        evaluateRuleSetResponse.Outcome.Action.Should().Be(RuleAction.Review);
        ((int)evaluateRuleSetResponse.Outcome.RiskScore).Should().Be(20);

        _mockCaseService.Verify(x => x.CreateAsync(It.IsAny<CreateCaseRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EvaluateAsync_AllFiredRulesAreFlags_DoesNotCreateCase()
    {
        // Arrange
        var rule = FiringRule(RuleAction.Flag, RuleSeverity.Low);

        var ruleSet = FakeRuleSet.CreateValid(Fixture) with
        {
            RuleSetId = rule.RuleSetId,
            ReviewThreshold = new ReviewThreshold(1000),
            Rules = [rule]
        };

        var evaluateRuleSetRequest = FakeEvaluateRuleSetRequest.CreateValid(Fixture) with
        {
            RuleSetId = ruleSet.RuleSetId,
            CreateCase = new CreateCase(true)
        };

        _mockRuleSetRepository
            .Setup(x => x.GetAsync(It.IsAny<RuleSetId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ruleSet);

        _mockRuleEvaluationService
            .Setup(x => x.CreateManyAsync(It.IsAny<List<CreateRuleEvaluationRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([FakeRuleEvaluation.CreateValid(Fixture) with { Matched = new Matched(true) }]);

        // Act
        var evaluateRuleSetResponse = await _sut.EvaluateAsync(evaluateRuleSetRequest, CancellationToken.None);

        // Assert
        evaluateRuleSetResponse.Results.First().Matched.Should().Be(new Matched(true));
        evaluateRuleSetResponse.Outcome.Action.Should().Be(RuleAction.Flag);
        evaluateRuleSetResponse.CaseId.Should().BeNull();

        _mockCaseService.Verify(x => x.CreateAsync(It.IsAny<CreateCaseRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EvaluateAsync_SeveralRulesFire_OpensOneCaseCarryingTheHighestSeverity()
    {
        // Arrange
        var lowRule = FiringRule(RuleAction.Review, RuleSeverity.Low);
        var highRule = FiringRule(RuleAction.Review, RuleSeverity.High) with { RuleSetId = lowRule.RuleSetId };
        var mediumRule = FiringRule(RuleAction.Review, RuleSeverity.Medium) with { RuleSetId = lowRule.RuleSetId };

        var ruleSet = FakeRuleSet.CreateValid(Fixture) with
        {
            RuleSetId = lowRule.RuleSetId,
            ReviewThreshold = new ReviewThreshold(1000),
            Rules = [lowRule, highRule, mediumRule]
        };

        var evaluateRuleSetRequest = FakeEvaluateRuleSetRequest.CreateValid(Fixture) with
        {
            RuleSetId = ruleSet.RuleSetId,
            CreateCase = new CreateCase(true)
        };

        var ruleEvaluations = ruleSet.Rules.ConvertAll(_ => FakeRuleEvaluation.CreateValid(Fixture) with { Matched = new Matched(true) });

        CreateCaseRequest? capturedCaseRequest = null;

        _mockRuleSetRepository
            .Setup(x => x.GetAsync(It.IsAny<RuleSetId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ruleSet);

        _mockRuleEvaluationService
            .Setup(x => x.CreateManyAsync(It.IsAny<List<CreateRuleEvaluationRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ruleEvaluations);

        _mockCaseService
            .Setup(x => x.CreateAsync(It.IsAny<CreateCaseRequest>(), It.IsAny<CancellationToken>()))
            .Callback<CreateCaseRequest, CancellationToken>((r, _) => capturedCaseRequest = r)
            .ReturnsAsync(FakeCase.CreateValid(Fixture));

        // Act
        await _sut.EvaluateAsync(evaluateRuleSetRequest, CancellationToken.None);

        // Assert
        capturedCaseRequest.Should().NotBeNull();
        capturedCaseRequest!.Severity.Should().Be(RuleSeverity.High);
        capturedCaseRequest.RuleEvaluationIds.Should().Equal(ruleEvaluations.ConvertAll(x => x.RuleEvaluationId));

        _mockCaseService.Verify(x => x.CreateAsync(It.IsAny<CreateCaseRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EvaluateAsync_OnlyMatchingEvaluationsAreLinkedToTheCase()
    {
        // Arrange
        var firingRule = FiringRule(RuleAction.Review, RuleSeverity.High);
        var quietRule = QuietRule() with { RuleSetId = firingRule.RuleSetId };

        var ruleSet = FakeRuleSet.CreateValid(Fixture) with
        {
            RuleSetId = firingRule.RuleSetId,
            ReviewThreshold = new ReviewThreshold(1000),
            Rules = [firingRule, quietRule]
        };

        var evaluateRuleSetRequest = FakeEvaluateRuleSetRequest.CreateValid(Fixture) with
        {
            RuleSetId = ruleSet.RuleSetId,
            CreateCase = new CreateCase(true)
        };

        var matchedEvaluation = FakeRuleEvaluation.CreateValid(Fixture) with { Matched = new Matched(true) };
        var unmatchedEvaluation = FakeRuleEvaluation.CreateValid(Fixture) with { Matched = new Matched(false) };

        CreateCaseRequest? capturedCaseRequest = null;

        _mockRuleSetRepository
            .Setup(x => x.GetAsync(It.IsAny<RuleSetId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ruleSet);

        _mockRuleEvaluationService
            .Setup(x => x.CreateManyAsync(It.IsAny<List<CreateRuleEvaluationRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([matchedEvaluation, unmatchedEvaluation]);

        _mockCaseService
            .Setup(x => x.CreateAsync(It.IsAny<CreateCaseRequest>(), It.IsAny<CancellationToken>()))
            .Callback<CreateCaseRequest, CancellationToken>((r, _) => capturedCaseRequest = r)
            .ReturnsAsync(FakeCase.CreateValid(Fixture));

        // Act
        await _sut.EvaluateAsync(evaluateRuleSetRequest, CancellationToken.None);

        // Assert
        capturedCaseRequest.Should().NotBeNull();
        capturedCaseRequest!.RuleEvaluationIds.Should().Equal(matchedEvaluation.RuleEvaluationId);
    }

    [Fact]
    public async Task EvaluateAsync_RecordsConditionMatchesOnlyForRulesThatFired()
    {
        // Arrange
        var firingRule = FiringRule(RuleAction.Review, RuleSeverity.High);
        var quietRule = QuietRule() with { RuleSetId = firingRule.RuleSetId };

        var ruleSet = FakeRuleSet.CreateValid(Fixture) with
        {
            RuleSetId = firingRule.RuleSetId,
            ReviewThreshold = new ReviewThreshold(1000),
            Rules = [firingRule, quietRule]
        };

        var evaluateRuleSetRequest = FakeEvaluateRuleSetRequest.CreateValid(Fixture) with
        {
            RuleSetId = ruleSet.RuleSetId,
            CreateCase = new CreateCase(false)
        };

        List<CreateRuleEvaluationRequest>? capturedEvaluationRequests = null;

        _mockRuleSetRepository
            .Setup(x => x.GetAsync(It.IsAny<RuleSetId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ruleSet);

        _mockRuleEvaluationService
            .Setup(x => x.CreateManyAsync(It.IsAny<List<CreateRuleEvaluationRequest>>(), It.IsAny<CancellationToken>()))
            .Callback<List<CreateRuleEvaluationRequest>, CancellationToken>((r, _) => capturedEvaluationRequests = r)
            .ReturnsAsync([FakeRuleEvaluation.CreateValid(Fixture) with { Matched = new Matched(true) }]);

        // Act
        await _sut.EvaluateAsync(evaluateRuleSetRequest, CancellationToken.None);

        // Assert
        capturedEvaluationRequests.Should().HaveCount(2);

        var fired = capturedEvaluationRequests!.Single(x => x.RuleId == firingRule.RuleId);
        fired.ConditionMatches.Should().ContainSingle();
        fired.ConditionMatches![0].Field.Should().Be("Key");
        fired.ConditionMatches[0].Matched.Should().BeTrue();

        capturedEvaluationRequests!.Single(x => x.RuleId == quietRule.RuleId).ConditionMatches.Should().BeNull();
    }

    private Rule QuietRule()
    {
        return FiringRule(RuleAction.Review, RuleSeverity.Low) with
        {
            RuleBody = new RuleBody
            {
                Condition = RuleCondition.EqualTo,
                Field = new RuleField("Key"),
                Value = new RuleValue("SomethingElse"),
                And = null,
                Or = null,
                Not = null
            }
        };
    }

    private Rule FiringRule(RuleAction action, RuleSeverity severity, int riskWeight = 0)
    {
        return FakeRule.CreateValid(Fixture) with
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
            Severity = severity,
            RiskWeight = new RiskWeight(riskWeight),
            IsEnabled = new IsEnabled(true),
            IsDeleted = new IsDeleted(false)
        };
    }
}