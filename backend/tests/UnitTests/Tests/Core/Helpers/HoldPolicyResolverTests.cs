using Core.Configs;
using Core.Enums;
using Core.Helpers;
using Core.Models.Organizations;
using Core.Models.Rules;
using Core.ValueObjects;
using FluentAssertions;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Organizations;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Rules;

namespace UnitTests.Tests.Core.Helpers;

public class HoldPolicyResolverTests : BaseTestClass
{
    private static readonly DecisionConfig DecisionConfig = new()
    {
        DefaultHoldWindowMinutes = 1440,
        DefaultHoldExpiryVerdict = HoldExpiryVerdict.Block
    };

    [Fact]
    public void Resolve_NoRulesFiredAndNoOrganizationPolicy_FallsBackToConfiguredDefault()
    {
        // Act
        var holdPolicy = HoldPolicyResolver.Resolve([], null, DecisionConfig);

        // Assert
        ((int)holdPolicy.HoldWindow!.Value).Should().Be(1440);
        holdPolicy.HoldExpiryVerdict.Should().Be(HoldExpiryVerdict.Block);
    }

    [Fact]
    public void Resolve_NoRulePolicy_FallsBackToOrganization()
    {
        // Arrange
        var organization = Organization(60, HoldExpiryVerdict.Release);

        // Act
        var holdPolicy = HoldPolicyResolver.Resolve([Rule(null, null)], organization, DecisionConfig);

        // Assert
        ((int)holdPolicy.HoldWindow!.Value).Should().Be(60);
        holdPolicy.HoldExpiryVerdict.Should().Be(HoldExpiryVerdict.Release);
    }

    [Fact]
    public void Resolve_RulePolicyPresent_OverridesOrganization()
    {
        // Arrange
        var organization = Organization(60, HoldExpiryVerdict.Release);

        // Act
        var holdPolicy = HoldPolicyResolver.Resolve([Rule(30, HoldExpiryVerdict.Expire)], organization, DecisionConfig);

        // Assert
        ((int)holdPolicy.HoldWindow!.Value).Should().Be(30);
        holdPolicy.HoldExpiryVerdict.Should().Be(HoldExpiryVerdict.Expire);
    }

    [Fact]
    public void Resolve_SeveralRulesFired_TakesTheLongestWindowAndTheMostRestrictiveVerdict()
    {
        // Arrange
        var firedRules = new List<Rule>
        {
            Rule(30, HoldExpiryVerdict.Release),
            Rule(4320, HoldExpiryVerdict.Expire),
            Rule(120, HoldExpiryVerdict.Block)
        };

        // Act
        var holdPolicy = HoldPolicyResolver.Resolve(firedRules, Organization(60, HoldExpiryVerdict.Release), DecisionConfig);

        // Assert
        ((int)holdPolicy.HoldWindow!.Value).Should().Be(4320);
        holdPolicy.HoldExpiryVerdict.Should().Be(HoldExpiryVerdict.Block);
    }

    [Fact]
    public void Resolve_OneFiredRuleNeverExpires_LeavesNoDeadlineWhateverElseFired()
    {
        // Arrange
        var firedRules = new List<Rule>
        {
            Rule(30, HoldExpiryVerdict.Block),
            Rule(120, HoldExpiryVerdict.Never)
        };

        // Act
        var holdPolicy = HoldPolicyResolver.Resolve(firedRules, Organization(60, HoldExpiryVerdict.Release), DecisionConfig);

        // Assert
        holdPolicy.HoldWindow.Should().BeNull();
        holdPolicy.HoldExpiryVerdict.Should().Be(HoldExpiryVerdict.Never);
    }

    [Fact]
    public void Resolve_OrganizationOptsOutOfExpiry_LeavesNoDeadline()
    {
        // Arrange
        var organization = FakeOrganization.CreateValid(Fixture) with
        {
            HoldWindow = null,
            HoldExpiryVerdict = HoldExpiryVerdict.Never
        };

        // Act
        var holdPolicy = HoldPolicyResolver.Resolve([Rule(null, null)], organization, DecisionConfig);

        // Assert
        holdPolicy.HoldWindow.Should().BeNull();
        holdPolicy.HoldExpiryVerdict.Should().Be(HoldExpiryVerdict.Never);
    }

    [Fact]
    public void Resolve_OrganizationOptsOutButARuleSetsAVerdict_LetsTheRuleReinstateTheDeadline()
    {
        // Arrange
        var organization = FakeOrganization.CreateValid(Fixture) with
        {
            HoldWindow = null,
            HoldExpiryVerdict = HoldExpiryVerdict.Never
        };

        // Act
        var holdPolicy = HoldPolicyResolver.Resolve([Rule(45, HoldExpiryVerdict.Block)], organization, DecisionConfig);

        // Assert
        ((int)holdPolicy.HoldWindow!.Value).Should().Be(45);
        holdPolicy.HoldExpiryVerdict.Should().Be(HoldExpiryVerdict.Block);
    }

    [Fact]
    public void Resolve_WindowAndVerdictSetOnDifferentRules_ResolvesEachIndependently()
    {
        // Arrange
        var firedRules = new List<Rule>
        {
            Rule(90, null),
            Rule(null, HoldExpiryVerdict.Release)
        };

        // Act
        var holdPolicy = HoldPolicyResolver.Resolve(firedRules, Organization(60, HoldExpiryVerdict.Block), DecisionConfig);

        // Assert
        ((int)holdPolicy.HoldWindow!.Value).Should().Be(90);
        holdPolicy.HoldExpiryVerdict.Should().Be(HoldExpiryVerdict.Release);
    }

    private Rule Rule(int? holdWindowMinutes, HoldExpiryVerdict? holdExpiryVerdict)
    {
        return FakeRule.CreateValid(Fixture) with
        {
            HoldWindow = holdWindowMinutes.HasValue ? new HoldWindow(holdWindowMinutes.Value) : null,
            HoldExpiryVerdict = holdExpiryVerdict
        };
    }

    private Organization Organization(int holdWindowMinutes, HoldExpiryVerdict holdExpiryVerdict)
    {
        return FakeOrganization.CreateValid(Fixture) with
        {
            HoldWindow = new HoldWindow(holdWindowMinutes),
            HoldExpiryVerdict = holdExpiryVerdict
        };
    }
}
