using System.Text.Json.Nodes;
using Core.Enums;
using Core.Exceptions;
using Core.Helpers;
using Core.Models.Rules;
using Core.ValueObjects;
using FluentAssertions;
using UnitTests.TestHelpers;

namespace UnitTests.Tests.Core.Helpers;

public class SandboxTriggerResolverTests : BaseTestClass
{
    [Theory]
    [InlineData("tm_test_allow", SandboxTrigger.Allow)]
    [InlineData("tm_test_flag", SandboxTrigger.Flag)]
    [InlineData("tm_test_review", SandboxTrigger.Review)]
    [InlineData("tm_test_hold", SandboxTrigger.Hold)]
    [InlineData("tm_test_block", SandboxTrigger.Block)]
    [InlineData("tm_test_error", SandboxTrigger.Error)]
    public void Resolve_GivenAReservedValueInTestMode_ReturnsTheTrigger(string value, SandboxTrigger expected)
    {
        // Arrange
        var payload = new JsonObject { ["reference"] = value };

        // Act
        var trigger = SandboxTriggerResolver.Resolve(Mode.Test, payload, null);

        // Assert
        trigger.Should().Be(expected);
    }

    [Fact]
    public void Resolve_GivenAReservedValueInLiveMode_ReturnsNothing()
    {
        // Arrange
        var payload = new JsonObject { ["reference"] = "tm_test_block" };

        // Act
        var trigger = SandboxTriggerResolver.Resolve(Mode.Live, payload, null);

        // Assert
        trigger.Should().BeNull();
    }

    [Fact]
    public void Resolve_GivenAReservedValueNested_FindsIt()
    {
        // Arrange
        var payload = new JsonObject
        {
            ["remitter"] = new JsonObject
            {
                ["tags"] = new JsonArray("ordinary", "tm_test_hold")
            }
        };

        // Act
        var trigger = SandboxTriggerResolver.Resolve(Mode.Test, payload, null);

        // Assert
        trigger.Should().Be(SandboxTrigger.Hold);
    }

    [Fact]
    public void Resolve_GivenTwoReservedValues_TakesTheMoreSevere()
    {
        // Arrange
        var payload = new JsonObject
        {
            ["a"] = "tm_test_flag",
            ["b"] = "tm_test_block"
        };

        // Act
        var trigger = SandboxTriggerResolver.Resolve(Mode.Test, payload, null);

        // Assert
        trigger.Should().Be(SandboxTrigger.Block);
    }

    [Fact]
    public void Resolve_GivenTheExternalReference_ReadsItToo()
    {
        // Act
        var trigger = SandboxTriggerResolver.Resolve(Mode.Test, new JsonObject(), new ExternalReference("tm_test_review"));

        // Assert
        trigger.Should().Be(SandboxTrigger.Review);
    }

    [Fact]
    public void Resolve_GivenNoReservedValue_ReturnsNothing()
    {
        // Arrange
        var payload = new JsonObject { ["reference"] = "an ordinary payment" };

        // Act
        var trigger = SandboxTriggerResolver.Resolve(Mode.Test, payload, new ExternalReference("ref-1"));

        // Assert
        trigger.Should().BeNull();
    }

    [Theory]
    [InlineData(SandboxTrigger.Allow, RuleAction.Allow)]
    [InlineData(SandboxTrigger.Flag, RuleAction.Flag)]
    [InlineData(SandboxTrigger.Review, RuleAction.Review)]
    [InlineData(SandboxTrigger.Hold, RuleAction.Review)]
    [InlineData(SandboxTrigger.Block, RuleAction.Block)]
    public void Apply_GivenATrigger_ForcesTheAction(SandboxTrigger trigger, RuleAction expected)
    {
        // Arrange
        var outcome = new RuleOutcome
        {
            Action = RuleAction.Allow,
            Severity = RuleSeverity.Info,
            RiskScore = new RiskScore(0)
        };

        // Act
        var forced = SandboxTriggerResolver.Apply(trigger, outcome);

        // Assert
        forced.Action.Should().Be(expected);
    }

    [Fact]
    public void Apply_GivenANonAllowTrigger_RaisesSeverityToAtLeastMedium()
    {
        // Arrange
        var outcome = new RuleOutcome
        {
            Action = RuleAction.Allow,
            Severity = RuleSeverity.Info,
            RiskScore = new RiskScore(0)
        };

        // Act
        var forced = SandboxTriggerResolver.Apply(SandboxTrigger.Block, outcome);

        // Assert
        forced.Severity.Should().Be(RuleSeverity.Medium);
    }

    [Fact]
    public void Apply_GivenASeverityAlreadyHigher_LeavesIt()
    {
        // Arrange
        var outcome = new RuleOutcome
        {
            Action = RuleAction.Review,
            Severity = RuleSeverity.Critical,
            RiskScore = new RiskScore(90)
        };

        // Act
        var forced = SandboxTriggerResolver.Apply(SandboxTrigger.Block, outcome);

        // Assert
        forced.Severity.Should().Be(RuleSeverity.Critical);
    }

    [Fact]
    public void ForcesCase_GivenHold_IsTrue()
    {
        // Assert
        SandboxTriggerResolver.ForcesCase(SandboxTrigger.Hold).Should().BeTrue();
        SandboxTriggerResolver.ForcesCase(SandboxTrigger.Block).Should().BeFalse();
    }

    [Fact]
    public void ThrowIfError_GivenTheErrorTriggerInTestMode_Throws()
    {
        // Arrange
        var payload = new JsonObject { ["reference"] = "tm_test_error" };

        // Act
        var act = () => SandboxTriggerResolver.ThrowIfError(Mode.Test, payload, null);

        // Assert
        act.Should().Throw<SandboxTriggerException>();
    }

    [Fact]
    public void ThrowIfError_GivenTheErrorTriggerInLiveMode_DoesNotThrow()
    {
        // Arrange
        var payload = new JsonObject { ["reference"] = "tm_test_error" };

        // Act
        var act = () => SandboxTriggerResolver.ThrowIfError(Mode.Live, payload, null);

        // Assert
        act.Should().NotThrow();
    }
}
