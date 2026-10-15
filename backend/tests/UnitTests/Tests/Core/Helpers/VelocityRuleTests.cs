using System.Text.Json.Nodes;
using Core.Enums;
using Core.Helpers;
using Core.Models.Parties;
using Core.Models.Rules;
using Core.Models.VelocityObservations;
using Core.ValueObjects;
using FluentAssertions;
using UnitTests.TestHelpers;

namespace UnitTests.Tests.Core.Helpers;

public class VelocityRuleTests : BaseTestClass
{
    private static readonly DateTimeOffset Now = new(2026, 8, 19, 14, 30, 0, TimeSpan.Zero);
    private static readonly JsonObject EmptyPayload = new();
    private static readonly PartyId Remitter = new(Guid.NewGuid());
    private static readonly PartyId OtherRemitter = new(Guid.NewGuid());

    [Fact]
    public void Evaluate_GivenASumOverTheRollingWindowAboveTheLimit_Matches()
    {
        // Arrange
        var rule = VelocityRule(VelocityFunction.Sum, RuleCondition.GreaterThan, 10000, includeCurrent: true);
        var facts = Facts(rule, Observation(Remitter, 6000m, Now.AddHours(-2), false), Observation(Remitter, 5000m, Now, true));

        // Act
        var matched = RuleHelper.Evaluate(rule, EmptyPayload, Now, Parties(Remitter), facts);

        // Assert
        matched.Should().BeTrue();
    }

    [Fact]
    public void Evaluate_GivenTheSameWindowExcludingTheCurrentPayment_DoesNotMatch()
    {
        // Arrange
        var rule = VelocityRule(VelocityFunction.Sum, RuleCondition.GreaterThan, 10000, includeCurrent: false);
        var facts = Facts(rule, Observation(Remitter, 6000m, Now.AddHours(-2), false), Observation(Remitter, 5000m, Now, true));

        // Act
        var matched = RuleHelper.Evaluate(rule, EmptyPayload, Now, Parties(Remitter), facts);

        // Assert
        matched.Should().BeFalse();
    }

    [Fact]
    public void Evaluate_GivenAnObservationOlderThanTheWindow_LeavesItOut()
    {
        // Arrange
        var rule = VelocityRule(VelocityFunction.Sum, RuleCondition.GreaterThan, 10000, includeCurrent: true);
        var facts = Facts(rule, Observation(Remitter, 9000m, Now.AddDays(-3), false), Observation(Remitter, 5000m, Now, true));

        // Act
        var matched = RuleHelper.Evaluate(rule, EmptyPayload, Now, Parties(Remitter), facts);

        // Assert
        matched.Should().BeFalse();
    }

    [Fact]
    public void Evaluate_GivenAPartyWithNothingInTheWindow_CountsZero()
    {
        // Arrange
        var rule = VelocityRule(VelocityFunction.Count, RuleCondition.EqualTo, 0, includeCurrent: false);

        // Act
        var matched = RuleHelper.Evaluate(rule, EmptyPayload, Now, Parties(Remitter), Facts(rule));

        // Assert
        matched.Should().BeTrue();
    }

    [Fact]
    public void Evaluate_GivenNoPartiesAtAll_DoesNotMatch()
    {
        // Arrange
        var rule = VelocityRule(VelocityFunction.Count, RuleCondition.EqualTo, 0, includeCurrent: false);

        // Act
        var matched = RuleHelper.Evaluate(rule, EmptyPayload, Now, [], Facts(rule));

        // Assert
        matched.Should().BeFalse();
    }

    [Fact]
    public void Evaluate_GivenTwoPartiesInTheRole_WeighsEachSeparately()
    {
        // Arrange
        var rule = VelocityRule(VelocityFunction.Sum, RuleCondition.GreaterThan, 10000, includeCurrent: true, quantifier: RuleQuantifier.Any);
        var facts = Facts(rule, Observation(Remitter, 6000m, Now, true), Observation(OtherRemitter, 6000m, Now, true));

        // Act
        var matched = RuleHelper.Evaluate(rule, EmptyPayload, Now, Parties(Remitter, OtherRemitter), facts);

        // Assert
        matched.Should().BeFalse();
    }

    [Fact]
    public void Trace_GivenAVelocityLeaf_RecordsTheWindowItReadAndWhatItFound()
    {
        // Arrange
        var rule = VelocityRule(VelocityFunction.Sum, RuleCondition.GreaterThan, 10000, includeCurrent: true);
        var facts = Facts(rule, Observation(Remitter, 11000m, Now, true));

        // Act
        var trace = RuleHelper.Trace(rule, EmptyPayload, RuleMatchPolarity.MatchesWhenTrue, Now, Parties(Remitter), facts);

        // Assert
        trace.Should().ContainSingle();
        trace[0].Field.Should().Be("sum(velocity.remitter.amount, last 1 day, including current)");
        trace[0].Actual!.ToString().Should().Be("11000");
        trace[0].Matched.Should().BeTrue();
    }

    private static RuleBodyModel VelocityRule(
        VelocityFunction function,
        RuleCondition condition,
        object value,
        bool includeCurrent,
        RuleQuantifier? quantifier = null)
    {
        return new RuleBodyModel
        {
            Condition = condition,
            Quantifier = quantifier,
            Velocity = new VelocityConditionModel
            {
                Role = PartyRole.Remitter,
                Metric = "amount",
                Function = function,
                Window = new VelocityWindowModel { Kind = VelocityWindowKind.Rolling, Unit = VelocityWindowUnit.Day, Length = 1 },
                IncludeCurrent = includeCurrent
            },
            Value = value
        };
    }

    private static VelocityFacts Facts(RuleBodyModel rule, params VelocityObservationFact[] observations)
    {
        var thresholds = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var query = VelocityWindowResolver.Query(rule.Velocity!, thresholds, Now, null);

        var windows = observations
            .Select(x => x.PartyId)
            .Distinct()
            .Select(partyId => new VelocityWindowFact
            {
                Query = query,
                PartyId = partyId,
                Aggregate = VelocityAggregator.Reduce(observations.Where(x => x.PartyId == partyId).ToList(), query)
            })
            .ToList();

        return new VelocityFacts
        {
            Windows = windows,
            Thresholds = thresholds,
            CurrentTransactionId = null
        };
    }

    private static VelocityObservationFact Observation(PartyId partyId, decimal number, DateTimeOffset observedAt, bool isCurrent)
    {
        return new VelocityObservationFact
        {
            PartyId = partyId,
            Role = PartyRole.Remitter,
            Metric = new VelocityMetricName("amount"),
            ObservedAt = new ObservedAt(observedAt),
            Text = new MetricText(number.ToString()),
            Number = new MetricNumber(number),
            IsCurrent = new IsCurrent(isCurrent)
        };
    }

    private static List<PartyFacts> Parties(params PartyId[] partyIds)
    {
        return partyIds.Select(x => new PartyFacts
        {
            PartyId = x,
            Role = PartyRole.Remitter,
            Identifier = new PartyIdentifier($"ACC-{x}"),
            RiskScore = null,
            FirstSeenAt = new FirstSeenAt(Now.AddDays(-100)),
            LastSeenAt = new LastSeenAt(Now)
        }).ToList();
    }

    [Fact]
    public void Evaluate_GivenTheMetricSpelledInADifferentCase_ReadsTheSameWindow()
    {
        // Arrange
        var declared = VelocityRule(VelocityFunction.Sum, RuleCondition.GreaterThan, 10000, includeCurrent: true);
        var facts = Facts(declared, Observation(Remitter, 6000m, Now.AddHours(-2), false), Observation(Remitter, 5000m, Now, true));
        var rule = declared with { Velocity = declared.Velocity! with { Metric = "Amount" } };

        // Act
        var matched = RuleHelper.Evaluate(rule, EmptyPayload, Now, Parties(Remitter), facts);

        // Assert
        matched.Should().BeTrue();
    }
}
