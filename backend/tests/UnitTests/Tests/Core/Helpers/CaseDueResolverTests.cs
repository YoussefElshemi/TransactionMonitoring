using Core.Configs;
using Core.Enums;
using Core.Helpers;
using Core.Models.Decisions;
using Core.ValueObjects;
using FluentAssertions;
using UnitTests.TestHelpers;

namespace UnitTests.Tests.Core.Helpers;

public class CaseDueResolverTests : BaseTestClass
{
    private static readonly DateTimeOffset OpenedAt = new(2026, 8, 20, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(RuleSeverity.Critical, 120)]
    [InlineData(RuleSeverity.High, 480)]
    [InlineData(RuleSeverity.Medium, 1440)]
    [InlineData(RuleSeverity.Low, 4320)]
    [InlineData(RuleSeverity.Info, 10080)]
    public void Resolve_WithNoDeclaredTargets_UsesTheHouseDefaultForTheSeverity(RuleSeverity severity, int expectedMinutes)
    {
        // Arrange
        var caseConfig = new CaseConfig();

        // Act
        var dueAt = CaseDueResolver.Resolve(severity, HoldPolicy(null), Targets(), caseConfig, OpenedAt);

        // Assert
        ((DateTimeOffset)dueAt).Should().Be(OpenedAt.AddMinutes(expectedMinutes));
    }

    [Fact]
    public void Resolve_WithADeclaredTarget_PrefersItOverTheHouseDefault()
    {
        // Arrange
        var targets = Targets((RuleSeverity.High, 30));

        // Act
        var dueAt = CaseDueResolver.Resolve(RuleSeverity.High, HoldPolicy(null), targets, new CaseConfig(), OpenedAt);

        // Assert
        ((DateTimeOffset)dueAt).Should().Be(OpenedAt.AddMinutes(30));
    }

    [Fact]
    public void Resolve_WithADeclaredTargetForAnotherBand_LeavesThisBandOnTheHouseDefault()
    {
        // Arrange
        var targets = Targets((RuleSeverity.Critical, 5));

        // Act
        var dueAt = CaseDueResolver.Resolve(RuleSeverity.High, HoldPolicy(null), targets, new CaseConfig(), OpenedAt);

        // Assert
        ((DateTimeOffset)dueAt).Should().Be(OpenedAt.AddMinutes(480));
    }

    [Fact]
    public void Resolve_WithASeverityTheHouseHasNoDefaultFor_FallsBackToTheFallback()
    {
        // Arrange
        var caseConfig = new CaseConfig
        {
            DefaultSlaTargetMinutes = [],
            FallbackSlaTargetMinutes = 90
        };

        // Act
        var dueAt = CaseDueResolver.Resolve(RuleSeverity.Critical, HoldPolicy(null), Targets(), caseConfig, OpenedAt);

        // Assert
        ((DateTimeOffset)dueAt).Should().Be(OpenedAt.AddMinutes(90));
    }

    [Fact]
    public void Resolve_WhenTheHoldExpiresFirst_CapsTheDeadlineAtTheHold()
    {
        // Act
        var dueAt = CaseDueResolver.Resolve(RuleSeverity.High, HoldPolicy(60), Targets(), new CaseConfig(), OpenedAt);

        // Assert
        ((DateTimeOffset)dueAt).Should().Be(OpenedAt.AddMinutes(60));
    }

    [Fact]
    public void Resolve_WhenTheTargetIsShorterThanTheHold_KeepsTheTarget()
    {
        // Act
        var dueAt = CaseDueResolver.Resolve(RuleSeverity.Critical, HoldPolicy(1440), Targets(), new CaseConfig(), OpenedAt);

        // Assert
        ((DateTimeOffset)dueAt).Should().Be(OpenedAt.AddMinutes(120));
    }

    [Fact]
    public void Resolve_WhenTheHoldNeverExpires_UsesTheTargetAlone()
    {
        // Arrange
        var holdPolicy = new HoldPolicy
        {
            HoldWindow = null,
            HoldExpiryVerdict = HoldExpiryVerdict.Never
        };

        // Act
        var dueAt = CaseDueResolver.Resolve(RuleSeverity.High, holdPolicy, Targets(), new CaseConfig(), OpenedAt);

        // Assert
        ((DateTimeOffset)dueAt).Should().Be(OpenedAt.AddMinutes(480));
    }

    [Fact]
    public void Resolve_WithNoHoldPolicyAtAll_UsesTheTargetAlone()
    {
        // Act
        var dueAt = CaseDueResolver.Resolve(RuleSeverity.Medium, null, Targets(), new CaseConfig(), OpenedAt);

        // Assert
        ((DateTimeOffset)dueAt).Should().Be(OpenedAt.AddMinutes(1440));
    }

    private static HoldPolicy HoldPolicy(int? holdWindowMinutes)
    {
        return new HoldPolicy
        {
            HoldWindow = holdWindowMinutes.HasValue ? new HoldWindow(holdWindowMinutes.Value) : null,
            HoldExpiryVerdict = HoldExpiryVerdict.Block
        };
    }

    private static Dictionary<RuleSeverity, SlaTarget> Targets(params (RuleSeverity Severity, int Minutes)[] targets)
    {
        return targets.ToDictionary(x => x.Severity, x => new SlaTarget(x.Minutes));
    }
}
