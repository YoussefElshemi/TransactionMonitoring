using Core.Enums;
using Core.Mappers;
using Core.Models.Rules;
using Core.ValueObjects;
using FluentAssertions;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Decisions;

namespace UnitTests.Tests.Core.Mappers;

public class RecordDecisionRequestMapperTests : BaseTestClass
{
    [Fact]
    public void ToModel_RecordDecisionRequest()
    {
        // Arrange
        var recordDecisionRequest = FakeRecordDecisionRequest.CreateValid(Fixture);

        // Act
        var decision = recordDecisionRequest.ToModel(MockTimeProvider, Mode.Live);

        // Assert
        decision.DecisionId.Should().NotBeNull();
        decision.OrganizationId.Should().Be(recordDecisionRequest.OrganizationId);
        decision.TransactionId.Should().Be(recordDecisionRequest.TransactionId);
        decision.ExternalReference.Should().BeNull();
        decision.CaseId.Should().Be(recordDecisionRequest.CaseId);
        decision.RuleEvaluations.Should().BeEmpty();
        decision.ResolvedByUserId.Should().BeNull();
        decision.ResolvedByUser.Should().BeNull();
        decision.CreatedAt.Should().Be(new CreatedAt(MockTimeProvider.GetUtcNow()));
        decision.UpdatedAt.Should().Be(new UpdatedAt(MockTimeProvider.GetUtcNow()));
    }

    [Theory]
    [InlineData(RuleAction.Block, true, DecisionStatus.Blocked, DecisionReasonCode.RuleBlocked)]
    [InlineData(RuleAction.Review, true, DecisionStatus.Held, DecisionReasonCode.RuleReviewRequired)]
    [InlineData(RuleAction.Flag, true, DecisionStatus.Released, DecisionReasonCode.RuleFlagged)]
    [InlineData(RuleAction.Allow, true, DecisionStatus.Released, DecisionReasonCode.RuleAllowed)]
    [InlineData(RuleAction.Allow, false, DecisionStatus.Released, DecisionReasonCode.NoRuleMatched)]
    public void ToModel_RecordDecisionRequest_TakesItsVerdictFromTheOutcome(
        RuleAction action,
        bool anyMatched,
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
            AnyMatched = new Matched(anyMatched)
        };

        // Act
        var decision = recordDecisionRequest.ToModel(MockTimeProvider, Mode.Live);

        // Assert
        decision.Status.Should().Be(expectedStatus);
        decision.ReasonCode.Should().Be(expectedReasonCode);
    }

    [Fact]
    public void ToModel_RecordDecisionRequest_HeldStampsHeldAtOnly()
    {
        // Arrange
        var recordDecisionRequest = FakeRecordDecisionRequest.CreateValid(Fixture) with
        {
            Outcome = new RuleOutcome
            {
                Action = RuleAction.Review,
                Severity = RuleSeverity.High,
                RiskScore = new RiskScore(10)
            }
        };

        // Act
        var decision = recordDecisionRequest.ToModel(MockTimeProvider, Mode.Live);

        // Assert
        decision.HeldAt.Should().Be(new HeldAt(MockTimeProvider.GetUtcNow()));
        decision.ResolvedAt.Should().BeNull();
    }

    [Theory]
    [InlineData(RuleAction.Block)]
    [InlineData(RuleAction.Allow)]
    public void ToModel_RecordDecisionRequest_TerminalStampsResolvedAtOnly(RuleAction action)
    {
        // Arrange
        var recordDecisionRequest = FakeRecordDecisionRequest.CreateValid(Fixture) with
        {
            Outcome = new RuleOutcome
            {
                Action = action,
                Severity = RuleSeverity.High,
                RiskScore = new RiskScore(10)
            }
        };

        // Act
        var decision = recordDecisionRequest.ToModel(MockTimeProvider, Mode.Live);

        // Assert
        decision.HeldAt.Should().BeNull();
        decision.ResolvedAt.Should().Be(new ResolvedAt(MockTimeProvider.GetUtcNow()));
    }

    [Fact]
    public void ToModel_RecordDecisionRequest_WithoutACase()
    {
        // Arrange
        var recordDecisionRequest = FakeRecordDecisionRequest.CreateValid(Fixture) with
        {
            CaseId = null
        };

        // Act
        var decision = recordDecisionRequest.ToModel(MockTimeProvider, Mode.Live);

        // Assert
        decision.CaseId.Should().BeNull();
    }
}
