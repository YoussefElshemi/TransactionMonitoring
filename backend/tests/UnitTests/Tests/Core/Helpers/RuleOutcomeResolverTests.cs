using Core.Enums;
using Core.Helpers;
using Core.Models.Rules;
using Core.ValueObjects;
using FluentAssertions;
using UnitTests.TestHelpers;

namespace UnitTests.Tests.Core.Helpers;

public class RuleOutcomeResolverTests : BaseTestClass
{
    private static RulePolicy Policy(RuleAction action, RuleSeverity severity = RuleSeverity.Info, int riskWeight = 0)
    {
        return new RulePolicy
        {
            Action = action,
            Severity = severity,
            RiskWeight = new RiskWeight(riskWeight)
        };
    }

    [Fact]
    public void Resolve_NoRulesFired_AllowsWithNoRisk()
    {
        // Arrange
        var threshold = new ReviewThreshold(10);

        // Act
        var outcome = RuleOutcomeResolver.Resolve([], threshold);

        // Assert
        outcome.Action.Should().Be(RuleAction.Allow);
        outcome.Severity.Should().Be(RuleSeverity.Info);
        ((int)outcome.RiskScore).Should().Be(0);
    }

    [Theory]
    [InlineData(RuleAction.Allow)]
    [InlineData(RuleAction.Flag)]
    [InlineData(RuleAction.Review)]
    [InlineData(RuleAction.Block)]
    public void Resolve_SingleRuleFired_ReturnsThatAction(RuleAction action)
    {
        // Arrange
        var threshold = new ReviewThreshold(100);

        // Act
        var outcome = RuleOutcomeResolver.Resolve([Policy(action)], threshold);

        // Assert
        outcome.Action.Should().Be(action);
    }

    [Theory]
    [InlineData(RuleAction.Allow, RuleAction.Flag, RuleAction.Flag)]
    [InlineData(RuleAction.Flag, RuleAction.Review, RuleAction.Review)]
    [InlineData(RuleAction.Review, RuleAction.Block, RuleAction.Block)]
    [InlineData(RuleAction.Block, RuleAction.Allow, RuleAction.Block)]
    public void Resolve_RulesDisagree_MostRestrictiveWins(RuleAction first, RuleAction second, RuleAction expected)
    {
        // Arrange
        var threshold = new ReviewThreshold(100);

        // Act
        var outcome = RuleOutcomeResolver.Resolve([Policy(first), Policy(second)], threshold);

        // Assert
        outcome.Action.Should().Be(expected);
    }

    [Fact]
    public void Resolve_RulesFired_TakesHighestSeverityIndependentlyOfAction()
    {
        // Arrange
        var threshold = new ReviewThreshold(100);
        var policies = new[]
        {
            Policy(RuleAction.Block, RuleSeverity.Low),
            Policy(RuleAction.Flag, RuleSeverity.Critical)
        };

        // Act
        var outcome = RuleOutcomeResolver.Resolve(policies, threshold);

        // Assert
        outcome.Action.Should().Be(RuleAction.Block);
        outcome.Severity.Should().Be(RuleSeverity.Critical);
    }

    [Fact]
    public void Resolve_RulesFired_SumsRiskWeights()
    {
        // Arrange
        var threshold = new ReviewThreshold(1000);
        var policies = new[]
        {
            Policy(RuleAction.Flag, riskWeight: 15),
            Policy(RuleAction.Flag, riskWeight: 25)
        };

        // Act
        var outcome = RuleOutcomeResolver.Resolve(policies, threshold);

        // Assert
        ((int)outcome.RiskScore).Should().Be(40);
    }

    [Fact]
    public void Resolve_RiskScoreReachesThreshold_EscalatesFlagToReview()
    {
        // Arrange
        var threshold = new ReviewThreshold(20);
        var policies = new[]
        {
            Policy(RuleAction.Flag, riskWeight: 10),
            Policy(RuleAction.Flag, riskWeight: 10)
        };

        // Act
        var outcome = RuleOutcomeResolver.Resolve(policies, threshold);

        // Assert
        outcome.Action.Should().Be(RuleAction.Review);
    }

    [Fact]
    public void Resolve_RiskScoreBelowThreshold_DoesNotEscalate()
    {
        // Arrange
        var threshold = new ReviewThreshold(20);
        var policies = new[]
        {
            Policy(RuleAction.Flag, riskWeight: 10),
            Policy(RuleAction.Flag, riskWeight: 9)
        };

        // Act
        var outcome = RuleOutcomeResolver.Resolve(policies, threshold);

        // Assert
        outcome.Action.Should().Be(RuleAction.Flag);
    }

    [Fact]
    public void Resolve_RiskScoreReachesThreshold_DoesNotDemoteBlock()
    {
        // Arrange
        var threshold = new ReviewThreshold(10);
        var policies = new[]
        {
            Policy(RuleAction.Block, riskWeight: 50)
        };

        // Act
        var outcome = RuleOutcomeResolver.Resolve(policies, threshold);

        // Assert
        outcome.Action.Should().Be(RuleAction.Block);
    }

    [Fact]
    public void Resolve_ZeroWeightRulesWithZeroThreshold_DoesNotEscalate()
    {
        // Arrange
        var threshold = new ReviewThreshold(0);
        var policies = new[]
        {
            Policy(RuleAction.Flag),
            Policy(RuleAction.Flag)
        };

        // Act
        var outcome = RuleOutcomeResolver.Resolve(policies, threshold);

        // Assert
        outcome.Action.Should().Be(RuleAction.Flag);
        ((int)outcome.RiskScore).Should().Be(0);
    }

    [Theory]
    [InlineData(RuleAction.Review, true)]
    [InlineData(RuleAction.Block, true)]
    [InlineData(RuleAction.Flag, false)]
    [InlineData(RuleAction.Allow, false)]
    public void OpensCase_ReturnsExpectedForEachAction(RuleAction action, bool expected)
    {
        // Act
        var opensCase = action.OpensCase();

        // Assert
        opensCase.Should().Be(expected);
    }
}
