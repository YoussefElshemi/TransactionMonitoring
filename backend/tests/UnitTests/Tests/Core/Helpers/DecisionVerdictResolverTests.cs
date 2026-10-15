using Core.Enums;
using Core.Helpers;
using FluentAssertions;

namespace UnitTests.Tests.Core.Helpers;

public class DecisionVerdictResolverTests
{
    [Theory]
    [InlineData(RuleAction.Block, true, true, DecisionStatus.Blocked, DecisionReasonCode.RuleBlocked)]
    [InlineData(RuleAction.Block, true, false, DecisionStatus.Blocked, DecisionReasonCode.RuleBlocked)]
    [InlineData(RuleAction.Review, true, true, DecisionStatus.Held, DecisionReasonCode.RuleReviewRequired)]
    [InlineData(RuleAction.Review, true, false, DecisionStatus.Released, DecisionReasonCode.RuleReviewRequired)]
    [InlineData(RuleAction.Flag, true, false, DecisionStatus.Released, DecisionReasonCode.RuleFlagged)]
    [InlineData(RuleAction.Allow, true, false, DecisionStatus.Released, DecisionReasonCode.RuleAllowed)]
    [InlineData(RuleAction.Allow, false, false, DecisionStatus.Released, DecisionReasonCode.NoRuleMatched)]
    public void ForAction_GivenAction_ResolvesVerdict(
        RuleAction action,
        bool anyMatched,
        bool hasCase,
        DecisionStatus expectedStatus,
        DecisionReasonCode expectedReasonCode)
    {
        // Act
        var verdict = DecisionVerdictResolver.ForAction(action, anyMatched, hasCase);

        // Assert
        verdict.Status.Should().Be(expectedStatus);
        verdict.ReasonCode.Should().Be(expectedReasonCode);
    }

    [Theory]
    [InlineData(CaseStatus.Approved, DecisionStatus.Released, DecisionReasonCode.AnalystApproved)]
    [InlineData(CaseStatus.Rejected, DecisionStatus.Blocked, DecisionReasonCode.AnalystRejected)]
    public void ForCaseStatus_GivenResolution_ResolvesVerdict(
        CaseStatus caseStatus,
        DecisionStatus expectedStatus,
        DecisionReasonCode expectedReasonCode)
    {
        // Act
        var verdict = DecisionVerdictResolver.ForCaseStatus(caseStatus);

        // Assert
        verdict.Status.Should().Be(expectedStatus);
        verdict.ReasonCode.Should().Be(expectedReasonCode);
    }

    [Theory]
    [InlineData(HoldExpiryVerdict.Release, DecisionStatus.Released)]
    [InlineData(HoldExpiryVerdict.Block, DecisionStatus.Blocked)]
    [InlineData(HoldExpiryVerdict.Expire, DecisionStatus.Expired)]
    public void ForHoldExpiry_GivenExpiryVerdict_SettlesUnderHoldExpired(
        HoldExpiryVerdict holdExpiryVerdict,
        DecisionStatus expectedStatus)
    {
        // Act
        var verdict = DecisionVerdictResolver.ForHoldExpiry(holdExpiryVerdict);

        // Assert
        verdict.Status.Should().Be(expectedStatus);
        verdict.ReasonCode.Should().Be(DecisionReasonCode.HoldExpired);
    }

    [Fact]
    public void ForHoldExpiry_GivenNever_Throws()
    {
        // Act
        var act = () => DecisionVerdictResolver.ForHoldExpiry(HoldExpiryVerdict.Never);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(DecisionStatus.Released, true)]
    [InlineData(DecisionStatus.Blocked, true)]
    [InlineData(DecisionStatus.Expired, true)]
    [InlineData(DecisionStatus.Held, false)]
    [InlineData(DecisionStatus.Pending, false)]
    public void IsTerminal_GivenStatus_ReportsWhetherDecisionIsFinal(DecisionStatus status, bool expected)
    {
        // Act
        var isTerminal = status.IsTerminal();

        // Assert
        isTerminal.Should().Be(expected);
    }
}
