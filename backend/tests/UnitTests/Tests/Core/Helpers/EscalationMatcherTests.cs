using System.Text.Json.Nodes;
using Core.Enums;
using Core.Helpers;
using Core.Models.Escalations;
using Core.Models.Parties;
using Core.Models.Rules;
using Core.ValueObjects;
using FluentAssertions;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Cases;

namespace UnitTests.Tests.Core.Helpers;

public class EscalationMatcherTests : BaseTestClass
{
    private static readonly DateTimeOffset Now = new(2026, 8, 20, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Matches_RuleWithNoConditions_MatchesNothing()
    {
        // Arrange
        var escalationRule = Rule();

        // Act
        var matches = EscalationMatcher.Matches(escalationRule, Candidate(), [], Now);

        // Assert
        matches.Should().BeFalse();
    }

    [Fact]
    public void Matches_UnassignedFor_MatchesOnlyOnceTheCaseHasWaitedThatLong()
    {
        // Arrange
        var escalationRule = Rule() with { UnassignedFor = new UnassignedFor(60) };

        // Act
        var waitedAnHour = EscalationMatcher.Matches(escalationRule, Candidate(openedMinutesAgo: 60), [], Now);
        var waitedHalfAnHour = EscalationMatcher.Matches(escalationRule, Candidate(openedMinutesAgo: 30), [], Now);

        // Assert
        waitedAnHour.Should().BeTrue();
        waitedHalfAnHour.Should().BeFalse();
    }

    [Fact]
    public void Matches_UnassignedFor_DoesNotMatchACaseSomebodyIsHolding()
    {
        // Arrange
        var escalationRule = Rule() with { UnassignedFor = new UnassignedFor(60) };
        var candidate = Candidate(openedMinutesAgo: 600) with
        {
            Case = Candidate(openedMinutesAgo: 600).Case with
            {
                AssignedToUserId = new ApplicationUserId(Guid.NewGuid())
            }
        };

        // Act
        var matches = EscalationMatcher.Matches(escalationRule, candidate, [], Now);

        // Assert
        matches.Should().BeFalse();
    }

    [Fact]
    public void Matches_HoldExpiryWithin_MatchesOnceTheDeadlineIsInsideTheWindow()
    {
        // Arrange
        var escalationRule = Rule() with { HoldExpiryWithin = new HoldExpiryWithin(30) };

        // Act
        var expiringSoon = EscalationMatcher.Matches(escalationRule, Candidate(holdExpiresInMinutes: 20), [], Now);
        var expiringLater = EscalationMatcher.Matches(escalationRule, Candidate(holdExpiresInMinutes: 90), [], Now);

        // Assert
        expiringSoon.Should().BeTrue();
        expiringLater.Should().BeFalse();
    }

    [Fact]
    public void Matches_HoldExpiryWithin_StillMatchesOnceTheDeadlineHasPassed()
    {
        // Arrange
        var escalationRule = Rule() with { HoldExpiryWithin = new HoldExpiryWithin(30) };

        // Act
        var matches = EscalationMatcher.Matches(escalationRule, Candidate(holdExpiresInMinutes: -120), [], Now);

        // Assert
        matches.Should().BeTrue();
    }

    [Fact]
    public void Matches_HoldExpiryWithin_DoesNotMatchACaseHoldingNothingThatExpires()
    {
        // Arrange
        var escalationRule = Rule() with { HoldExpiryWithin = new HoldExpiryWithin(30) };

        // Act
        var matches = EscalationMatcher.Matches(escalationRule, Candidate(), [], Now);

        // Assert
        matches.Should().BeFalse();
    }

    [Theory]
    [InlineData(RuleSeverity.Critical, true)]
    [InlineData(RuleSeverity.High, true)]
    [InlineData(RuleSeverity.Medium, false)]
    public void Matches_MinimumSeverity_IsAFloorAndNotAnEquality(RuleSeverity severity, bool expected)
    {
        // Arrange
        var escalationRule = Rule() with { MinimumSeverity = RuleSeverity.High };

        // Act
        var matches = EscalationMatcher.Matches(escalationRule, Candidate(severity: severity), [], Now);

        // Assert
        matches.Should().Be(expected);
    }

    [Fact]
    public void Matches_Matcher_ReadsTheAmountOffThePayload()
    {
        // Arrange
        var escalationRule = Rule() with
        {
            Matcher = Leaf("payment.amount", RuleCondition.GreaterThan, 10000m)
        };

        // Act
        var large = EscalationMatcher.Matches(escalationRule, Candidate(amount: 50000m), [], Now);
        var small = EscalationMatcher.Matches(escalationRule, Candidate(amount: 100m), [], Now);

        // Assert
        large.Should().BeTrue();
        small.Should().BeFalse();
    }

    [Fact]
    public void Matches_Matcher_CanAskAboutThePartiesAsTheyStandNow()
    {
        // Arrange
        var escalationRule = Rule() with
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
        var risky = EscalationMatcher.Matches(escalationRule, Candidate(), [Party(90)], Now);
        var safe = EscalationMatcher.Matches(escalationRule, Candidate(), [Party(10)], Now);

        // Assert
        risky.Should().BeTrue();
        safe.Should().BeFalse();
    }

    [Fact]
    public void Matches_SeveralConditions_RequiresAllOfThem()
    {
        // Arrange
        var escalationRule = Rule() with
        {
            UnassignedFor = new UnassignedFor(60),
            MinimumSeverity = RuleSeverity.High
        };

        // Act
        var both = EscalationMatcher.Matches(escalationRule, Candidate(openedMinutesAgo: 90, severity: RuleSeverity.Critical), [], Now);
        var ageOnly = EscalationMatcher.Matches(escalationRule, Candidate(openedMinutesAgo: 90, severity: RuleSeverity.Low), [], Now);

        // Assert
        both.Should().BeTrue();
        ageOnly.Should().BeFalse();
    }

    [Fact]
    public void FirstMatch_TriesRulesInAscendingPriority()
    {
        // Arrange
        var first = Rule() with
        {
            Name = new EscalationRuleName("First"),
            Priority = new EscalationPriority(10),
            MinimumSeverity = RuleSeverity.Info
        };

        var second = Rule() with
        {
            Name = new EscalationRuleName("Second"),
            Priority = new EscalationPriority(20),
            MinimumSeverity = RuleSeverity.Info
        };

        // Act
        var matched = EscalationMatcher.FirstMatch([second, first], Candidate(), [], Now);

        // Assert
        matched!.Name.Should().Be(first.Name);
    }

    [Fact]
    public void FirstMatch_BreaksAPriorityTieOnAge()
    {
        // Arrange
        var older = Rule() with
        {
            Name = new EscalationRuleName("Older"),
            MinimumSeverity = RuleSeverity.Info,
            CreatedAt = new CreatedAt(Now.AddDays(-10))
        };

        var newer = Rule() with
        {
            Name = new EscalationRuleName("Newer"),
            MinimumSeverity = RuleSeverity.Info,
            CreatedAt = new CreatedAt(Now.AddDays(-1))
        };

        // Act
        var matched = EscalationMatcher.FirstMatch([newer, older], Candidate(), [], Now);

        // Assert
        matched!.Name.Should().Be(older.Name);
    }

    [Fact]
    public void FirstMatch_SkipsADisabledRule()
    {
        // Arrange
        var escalationRule = Rule() with
        {
            MinimumSeverity = RuleSeverity.Info,
            IsEnabled = new IsEnabled(false)
        };

        // Act
        var matched = EscalationMatcher.FirstMatch([escalationRule], Candidate(), [], Now);

        // Assert
        matched.Should().BeNull();
    }

    private static EscalationRule Rule()
    {
        return new EscalationRule
        {
            Mode = Mode.Live,
            EscalationRuleId = new EscalationRuleId(Guid.NewGuid()),
            OrganizationId = new OrganizationId(Guid.NewGuid()),
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
            CreatedAt = new CreatedAt(Now.AddDays(-1)),
            UpdatedAt = new UpdatedAt(Now.AddDays(-1)),
            IsDeleted = new IsDeleted(false),
            DeletedAt = null
        };
    }

    private EscalationCandidate Candidate(
        int openedMinutesAgo = 0,
        int? holdExpiresInMinutes = null,
        RuleSeverity severity = RuleSeverity.Medium,
        decimal amount = 100m)
    {
        var @case = FakeCase.CreateValid(Fixture) with
        {
            Status = CaseStatus.Pending,
            Severity = severity,
            AssignedToUserId = null,
            CreatedAt = new CreatedAt(Now.AddMinutes(-openedMinutesAgo)),
            DueAt = new DueAt(Now.AddHours(1)),
            EscalatedAt = null,
            EscalationRuleId = null,
            ResolvedAt = null
        };

        return new EscalationCandidate
        {
            Case = @case,
            Payload = Payload(amount),
            HoldExpiresAt = holdExpiresInMinutes.HasValue ? new ExpiresAt(Now.AddMinutes(holdExpiresInMinutes.Value)) : null
        };
    }

    private static JsonObject Payload(decimal amount)
    {
        return new JsonObject
        {
            ["payment"] = new JsonObject
            {
                ["amount"] = JsonValue.Create(amount)
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
