using System.Text.Json.Nodes;
using Core.Enums;
using Core.Helpers;
using Core.Models.Parties;
using Core.Models.Rules;
using Core.ValueObjects;
using FluentAssertions;
using UnitTests.TestHelpers;

namespace UnitTests.Tests.Core.Helpers;

public class PartyRuleTests : BaseTestClass
{
    private static readonly DateTimeOffset Now = new(2026, 8, 18, 12, 0, 0, TimeSpan.Zero);
    private static readonly JsonObject EmptyPayload = new();

    [Fact]
    public void Evaluate_GivenARiskScoreOverTheThreshold_Matches()
    {
        // Arrange
        var rule = PartyRule(PartyRole.Remitter, PartyAttribute.RiskScore, RuleCondition.GreaterThan, 70);
        var parties = new List<PartyFacts> { Facts(PartyRole.Remitter, riskScore: 80) };

        // Act
        var matched = RuleHelper.Evaluate(rule, EmptyPayload, Now, parties);

        // Assert
        matched.Should().BeTrue();
    }

    [Fact]
    public void Evaluate_GivenAnUnassessedParty_DoesNotMatch()
    {
        // Arrange
        var rule = PartyRule(PartyRole.Remitter, PartyAttribute.RiskScore, RuleCondition.GreaterThan, 70);
        var parties = new List<PartyFacts> { Facts(PartyRole.Remitter, riskScore: null) };

        // Act
        var matched = RuleHelper.Evaluate(rule, EmptyPayload, Now, parties);

        // Assert
        matched.Should().BeFalse();
    }

    [Fact]
    public void Evaluate_GivenARoleThePaymentCarriesNobodyIn_DoesNotMatch()
    {
        // Arrange
        var rule = PartyRule(PartyRole.Beneficiary, PartyAttribute.RiskScore, RuleCondition.GreaterThan, 70);
        var parties = new List<PartyFacts> { Facts(PartyRole.Remitter, riskScore: 90) };

        // Act
        var matched = RuleHelper.Evaluate(rule, EmptyPayload, Now, parties);

        // Assert
        matched.Should().BeFalse();
    }

    [Fact]
    public void Evaluate_GivenNoPartiesAtAll_DoesNotMatch()
    {
        // Arrange
        var rule = PartyRule(PartyRole.Remitter, PartyAttribute.RiskScore, RuleCondition.GreaterThan, 70);

        // Act
        var matched = RuleHelper.Evaluate(rule, EmptyPayload, Now);

        // Assert
        matched.Should().BeFalse();
    }

    [Fact]
    public void Evaluate_GivenAPartyFirstSeenOnThisPayment_ReadsZeroDaysSinceFirstSeen()
    {
        // Arrange
        var rule = PartyRule(PartyRole.Beneficiary, PartyAttribute.DaysSinceFirstSeen, RuleCondition.LessThan, 7);
        var parties = new List<PartyFacts> { Facts(PartyRole.Beneficiary, firstSeenAt: Now) };

        // Act
        var matched = RuleHelper.Evaluate(rule, EmptyPayload, Now, parties);

        // Assert
        matched.Should().BeTrue();
    }

    [Fact]
    public void Evaluate_GivenALongStandingParty_DoesNotMatchANewPartyRule()
    {
        // Arrange
        var rule = PartyRule(PartyRole.Beneficiary, PartyAttribute.DaysSinceFirstSeen, RuleCondition.LessThan, 7);
        var parties = new List<PartyFacts> { Facts(PartyRole.Beneficiary, firstSeenAt: Now.AddDays(-400)) };

        // Act
        var matched = RuleHelper.Evaluate(rule, EmptyPayload, Now, parties);

        // Assert
        matched.Should().BeFalse();
    }

    [Theory]
    [InlineData(RuleQuantifier.Any, true)]
    [InlineData(RuleQuantifier.All, false)]
    public void Evaluate_GivenManyPartiesInARole_HonoursTheQuantifier(RuleQuantifier quantifier, bool expected)
    {
        // Arrange
        var rule = PartyRule(PartyRole.Beneficiary, PartyAttribute.RiskScore, RuleCondition.GreaterThan, 70) with
        {
            Quantifier = quantifier
        };

        var parties = new List<PartyFacts>
        {
            Facts(PartyRole.Beneficiary, riskScore: 90),
            Facts(PartyRole.Beneficiary, riskScore: 10)
        };

        // Act
        var matched = RuleHelper.Evaluate(rule, EmptyPayload, Now, parties);

        // Assert
        matched.Should().Be(expected);
    }

    [Theory]
    [InlineData(RuleQuantifier.Any)]
    [InlineData(RuleQuantifier.All)]
    public void Evaluate_GivenAQuantifierOverNoParties_DoesNotMatch(RuleQuantifier quantifier)
    {
        // Arrange
        var rule = PartyRule(PartyRole.Beneficiary, PartyAttribute.RiskScore, RuleCondition.GreaterThan, 70) with
        {
            Quantifier = quantifier
        };

        // Act
        var matched = RuleHelper.Evaluate(rule, EmptyPayload, Now, []);

        // Assert
        matched.Should().BeFalse();
    }

    [Fact]
    public void Evaluate_GivenAnAggregateOverAPartyRole_ReducesEveryPartyInIt()
    {
        // Arrange
        var rule = new RuleBodyModel
        {
            Condition = RuleCondition.GreaterThan,
            Expression = new RuleExpressionModel
            {
                Function = RuleFunction.Sum,
                Arguments =
                [
                    new RuleExpressionModel
                    {
                        Party = new PartyConditionModel { Role = PartyRole.Beneficiary, Attribute = PartyAttribute.RiskScore }
                    }
                ]
            },
            Value = 100
        };

        var parties = new List<PartyFacts>
        {
            Facts(PartyRole.Beneficiary, riskScore: 60),
            Facts(PartyRole.Beneficiary, riskScore: 60)
        };

        // Act
        var matched = RuleHelper.Evaluate(rule, EmptyPayload, Now, parties);

        // Assert
        matched.Should().BeTrue();
    }

    [Fact]
    public void Trace_GivenAQuantifiedPartyRule_RecordsEveryPartyItWeighed()
    {
        // Arrange
        var rule = PartyRule(PartyRole.Beneficiary, PartyAttribute.RiskScore, RuleCondition.GreaterThan, 70) with
        {
            Quantifier = RuleQuantifier.Any
        };

        var parties = new List<PartyFacts>
        {
            Facts(PartyRole.Beneficiary, riskScore: 90),
            Facts(PartyRole.Beneficiary, riskScore: 10)
        };

        // Act
        var trace = RuleHelper.Trace(rule, EmptyPayload, RuleMatchPolarity.MatchesWhenTrue, Now, parties);

        // Assert
        trace.Should().ContainSingle();
        trace[0].Field.Should().Be("party.beneficiary.riskScore");
        trace[0].Matched.Should().BeTrue();
        trace[0].Actual!.AsArray().Should().HaveCount(2);
    }

    [Fact]
    public void Trace_GivenASinglePartyRule_RecordsTheValueItRead()
    {
        // Arrange
        var rule = PartyRule(PartyRole.Remitter, PartyAttribute.RiskScore, RuleCondition.GreaterThan, 70);
        var parties = new List<PartyFacts> { Facts(PartyRole.Remitter, riskScore: 80) };

        // Act
        var trace = RuleHelper.Trace(rule, EmptyPayload, RuleMatchPolarity.MatchesWhenTrue, Now, parties);

        // Assert
        trace.Should().ContainSingle();
        trace[0].Field.Should().Be("party.remitter.riskScore");
        trace[0].Actual!.GetValue<decimal>().Should().Be(80);
    }

    [Fact]
    public void Evaluate_GivenIsNullOnAnUnassessedParty_Matches()
    {
        // Arrange
        var rule = new RuleBodyModel
        {
            Condition = RuleCondition.IsNull,
            Party = new PartyConditionModel { Role = PartyRole.Remitter, Attribute = PartyAttribute.RiskScore }
        };

        var parties = new List<PartyFacts> { Facts(PartyRole.Remitter, riskScore: null) };

        // Act
        var matched = RuleHelper.Evaluate(rule, EmptyPayload, Now, parties);

        // Assert
        matched.Should().BeTrue();
    }

    private static RuleBodyModel PartyRule(PartyRole role, PartyAttribute attribute, RuleCondition condition, object value)
    {
        return new RuleBodyModel
        {
            Condition = condition,
            Party = new PartyConditionModel { Role = role, Attribute = attribute },
            Value = value
        };
    }

    private static PartyFacts Facts(PartyRole role, int? riskScore = null, DateTimeOffset? firstSeenAt = null)
    {
        return new PartyFacts
        {
            PartyId = new PartyId(Guid.NewGuid()),
            Role = role,
            Identifier = new PartyIdentifier($"ACC-{Guid.NewGuid():N}"),
            RiskScore = riskScore.HasValue ? new RiskScore(riskScore.Value) : null,
            FirstSeenAt = new FirstSeenAt(firstSeenAt ?? Now.AddDays(-100)),
            LastSeenAt = new LastSeenAt(Now)
        };
    }
}
