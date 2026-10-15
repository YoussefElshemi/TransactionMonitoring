using System.Text.Json.Nodes;
using Core.Enums;
using Core.Helpers;
using Core.Models.Parties;
using Core.Models.Queues;
using Core.Models.Rules;
using Core.ValueObjects;
using FluentAssertions;
using UnitTests.TestHelpers;

namespace UnitTests.Tests.Core.Helpers;

public class QueueMatcherTests : BaseTestClass
{
    private static readonly DateTimeOffset Now = new(2026, 8, 20, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Matches_QueueWithNoCriteria_TakesEverything()
    {
        // Arrange
        var queue = Queue();
        var request = Request(RuleSeverity.Info);

        // Act
        var matches = QueueMatcher.Matches(queue, request, Now);

        // Assert
        matches.Should().BeTrue();
    }

    [Theory]
    [InlineData(RuleSeverity.Critical, true)]
    [InlineData(RuleSeverity.High, true)]
    [InlineData(RuleSeverity.Medium, false)]
    [InlineData(RuleSeverity.Info, false)]
    public void Matches_MinimumSeverity_IsAFloorAndNotAnEquality(RuleSeverity severity, bool expected)
    {
        // Arrange
        var queue = Queue() with { MinimumSeverity = RuleSeverity.High };
        var request = Request(severity);

        // Act
        var matches = QueueMatcher.Matches(queue, request, Now);

        // Assert
        matches.Should().Be(expected);
    }

    [Fact]
    public void Matches_RuleIds_MatchOnAnyOfThem()
    {
        // Arrange
        var wanted = new RuleId(Guid.NewGuid());
        var other = new RuleId(Guid.NewGuid());
        var queue = Queue() with { RuleIds = [wanted, new RuleId(Guid.NewGuid())] };

        // Act
        var matchesWanted = QueueMatcher.Matches(queue, Request(RuleSeverity.High, [other, wanted]), Now);
        var matchesOther = QueueMatcher.Matches(queue, Request(RuleSeverity.High, [other]), Now);

        // Assert
        matchesWanted.Should().BeTrue();
        matchesOther.Should().BeFalse();
    }

    [Fact]
    public void Matches_AmountBand_ReadsThePayloadThroughTheRuleExpressionLanguage()
    {
        // Arrange
        var queue = Queue() with
        {
            Matcher = new RuleBody
            {
                Condition = null,
                Field = null,
                Value = null,
                And =
                [
                    Leaf("payment.amount", RuleCondition.GreaterThanEqual, 10000m),
                    Leaf("payment.amount", RuleCondition.LessThan, 50000m)
                ],
                Or = null,
                Not = null
            }
        };

        // Act
        var inBand = QueueMatcher.Matches(queue, Request(RuleSeverity.High, payload: Payload(25000m, "GB")), Now);
        var belowBand = QueueMatcher.Matches(queue, Request(RuleSeverity.High, payload: Payload(500m, "GB")), Now);
        var aboveBand = QueueMatcher.Matches(queue, Request(RuleSeverity.High, payload: Payload(90000m, "GB")), Now);

        // Assert
        inBand.Should().BeTrue();
        belowBand.Should().BeFalse();
        aboveBand.Should().BeFalse();
    }

    [Fact]
    public void Matches_Region_IsAMembershipTest()
    {
        // Arrange
        var queue = Queue() with
        {
            Matcher = Leaf("payment.debtor.country", RuleCondition.In, new List<object> { "GB", "DE", "FR" })
        };

        // Act
        var inRegion = QueueMatcher.Matches(queue, Request(RuleSeverity.High, payload: Payload(100m, "DE")), Now);
        var outOfRegion = QueueMatcher.Matches(queue, Request(RuleSeverity.High, payload: Payload(100m, "US")), Now);

        // Assert
        inRegion.Should().BeTrue();
        outOfRegion.Should().BeFalse();
    }

    [Fact]
    public void Matches_PartyRiskScore_IsARoutableCriterion()
    {
        // Arrange
        var queue = Queue() with
        {
            Matcher = new RuleBody
            {
                Condition = RuleCondition.GreaterThan,
                Field = null,
                Party = new PartyCondition { Role = PartyRole.Remitter, Attribute = PartyAttribute.RiskScore },
                Value = new RuleValue(80m),
                And = null,
                Or = null,
                Not = null
            }
        };

        // Act
        var risky = QueueMatcher.Matches(queue, Request(RuleSeverity.High, parties: [Party(95)]), Now);
        var ordinary = QueueMatcher.Matches(queue, Request(RuleSeverity.High, parties: [Party(10)]), Now);

        // Assert
        risky.Should().BeTrue();
        ordinary.Should().BeFalse();
    }

    [Fact]
    public void Matches_SeveralCriteria_MustAllBeSatisfied()
    {
        // Arrange
        var ruleId = new RuleId(Guid.NewGuid());
        var queue = Queue() with
        {
            MinimumSeverity = RuleSeverity.High,
            RuleIds = [ruleId],
            Matcher = Leaf("payment.debtor.country", RuleCondition.EqualTo, "GB")
        };

        // Act
        var all = QueueMatcher.Matches(queue, Request(RuleSeverity.Critical, [ruleId], Payload(1m, "GB")), Now);
        var wrongRegion = QueueMatcher.Matches(queue, Request(RuleSeverity.Critical, [ruleId], Payload(1m, "US")), Now);
        var wrongSeverity = QueueMatcher.Matches(queue, Request(RuleSeverity.Low, [ruleId], Payload(1m, "GB")), Now);

        // Assert
        all.Should().BeTrue();
        wrongRegion.Should().BeFalse();
        wrongSeverity.Should().BeFalse();
    }

    [Fact]
    public void FirstMatch_TriesQueuesInAscendingPriority()
    {
        // Arrange
        var catchAll = Queue() with { Name = new QueueName("Everything"), Priority = new QueuePriority(500) };
        var critical = Queue() with
        {
            Name = new QueueName("Critical"),
            Priority = new QueuePriority(10),
            MinimumSeverity = RuleSeverity.Critical
        };

        // Act
        var matched = QueueMatcher.FirstMatch([catchAll, critical], Request(RuleSeverity.Critical), Now);

        // Assert
        matched!.Name.ToString().Should().Be("Critical");
    }

    [Fact]
    public void FirstMatch_SamePriority_BreaksTheTieOnAge()
    {
        // Arrange
        var older = Queue() with { Name = new QueueName("Older"), CreatedAt = new CreatedAt(Now.AddDays(-2)) };
        var newer = Queue() with { Name = new QueueName("Newer"), CreatedAt = new CreatedAt(Now.AddDays(-1)) };

        // Act
        var matched = QueueMatcher.FirstMatch([newer, older], Request(RuleSeverity.High), Now);

        // Assert
        matched!.Name.ToString().Should().Be("Older");
    }

    [Fact]
    public void FirstMatch_DisabledQueue_IsNeverTried()
    {
        // Arrange
        var disabled = Queue() with { Priority = new QueuePriority(1), IsEnabled = new IsEnabled(false) };

        // Act
        var matched = QueueMatcher.FirstMatch([disabled], Request(RuleSeverity.High), Now);

        // Assert
        matched.Should().BeNull();
    }

    [Fact]
    public void FirstMatch_NothingMatches_LeavesTheCaseUnrouted()
    {
        // Arrange
        var queue = Queue() with { MinimumSeverity = RuleSeverity.Critical };

        // Act
        var matched = QueueMatcher.FirstMatch([queue], Request(RuleSeverity.Low), Now);

        // Assert
        matched.Should().BeNull();
    }

    [Fact]
    public void ContainsVelocity_FindsAVelocityConditionNestedInAComposite()
    {
        // Arrange
        var matcher = new RuleBody
        {
            Condition = null,
            Field = null,
            Value = null,
            And =
            [
                Leaf("payment.amount", RuleCondition.GreaterThan, 1m),
                new RuleBody
                {
                    Condition = RuleCondition.GreaterThan,
                    Field = null,
                    Value = new RuleValue(3m),
                    Velocity = new VelocityCondition
                    {
                        Role = PartyRole.Remitter,
                        Metric = new VelocityMetricName("count"),
                        Function = VelocityFunction.Count,
                        Window = new VelocityWindow
                        {
                            Kind = VelocityWindowKind.Rolling,
                            Unit = VelocityWindowUnit.Hour,
                            Length = new WindowLength(1),
                            TimeZone = null
                        },
                        IncludeCurrent = new IncludeCurrent(true),
                        Threshold = null,
                        Band = null
                    },
                    And = null,
                    Or = null,
                    Not = null
                }
            ],
            Or = null,
            Not = null
        };

        // Act
        var containsVelocity = RuleHelper.ContainsVelocity(matcher);

        // Assert
        containsVelocity.Should().BeTrue();
    }

    [Fact]
    public void ContainsVelocity_PlainPayloadMatcher_IsAccepted()
    {
        // Act
        var containsVelocity = RuleHelper.ContainsVelocity(Leaf("payment.amount", RuleCondition.GreaterThan, 1m));

        // Assert
        containsVelocity.Should().BeFalse();
    }

    private static Queue Queue()
    {
        return new Queue
        {
            Mode = Mode.Live,
            QueueId = new QueueId(Guid.NewGuid()),
            OrganizationId = new OrganizationId(Guid.NewGuid()),
            SubOrganizationId = null,
            Name = new QueueName("Queue"),
            Description = null,
            Priority = new QueuePriority(100),
            RoutingPolicy = QueueRoutingPolicy.Manual,
            MinimumSeverity = null,
            RuleIds = [],
            Matcher = null,
            IsEnabled = new IsEnabled(true),
            Members = [],
            CreatedAt = new CreatedAt(Now.AddDays(-1)),
            UpdatedAt = new UpdatedAt(Now.AddDays(-1)),
            IsDeleted = new IsDeleted(false),
            DeletedAt = null
        };
    }

    private static RouteCaseRequest Request(
        RuleSeverity severity,
        List<RuleId>? ruleIds = null,
        JsonObject? payload = null,
        List<PartyFacts>? parties = null)
    {
        return new RouteCaseRequest
        {
            OrganizationId = new OrganizationId(Guid.NewGuid()),
            Severity = severity,
            RuleIds = ruleIds ?? [],
            Payload = payload ?? Payload(100m, "GB"),
            Parties = parties ?? []
        };
    }

    private static JsonObject Payload(decimal amount, string country)
    {
        return new JsonObject
        {
            ["payment"] = new JsonObject
            {
                ["amount"] = JsonValue.Create(amount),
                ["debtor"] = new JsonObject
                {
                    ["country"] = JsonValue.Create(country)
                }
            }
        };
    }

    private static PartyFacts Party(int riskScore)
    {
        return new PartyFacts
        {
            PartyId = new PartyId(Guid.NewGuid()),
            Role = PartyRole.Remitter,
            Identifier = new PartyIdentifier("GB00TEST00000000000000"),
            RiskScore = new RiskScore(riskScore),
            FirstSeenAt = new FirstSeenAt(Now.AddDays(-30)),
            LastSeenAt = new LastSeenAt(Now)
        };
    }

    private static RuleBody Leaf(string field, RuleCondition condition, object value)
    {
        return new RuleBody
        {
            Condition = condition,
            Field = new RuleField(field),
            Value = new RuleValue(value),
            And = null,
            Or = null,
            Not = null
        };
    }
}
