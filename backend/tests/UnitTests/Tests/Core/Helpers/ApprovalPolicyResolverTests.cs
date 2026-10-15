using System.Text.Json.Nodes;
using Core.Enums;
using Core.Helpers;
using Core.Models.Approvals;
using Core.Models.Parties;
using Core.Models.Rules;
using Core.ValueObjects;
using FluentAssertions;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Approvals;

namespace UnitTests.Tests.Core.Helpers;

public class ApprovalPolicyResolverTests : BaseTestClass
{
    private static readonly DateTimeOffset Now = new(2026, 8, 21, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void RequiresDeploymentApproval_NoPolicy_IsFalse()
    {
        // Arrange
        ApprovalPolicy? approvalPolicy = null;

        // Act
        var requiresApproval = ApprovalPolicyResolver.RequiresDeploymentApproval(approvalPolicy);

        // Assert
        requiresApproval.Should().BeFalse();
    }

    [Fact]
    public void RequiresDeploymentApproval_PolicyStatingNo_IsFalse()
    {
        // Arrange
        var approvalPolicy = Policy() with { RequireDeploymentApproval = new IsRequired(false) };

        // Act
        var requiresApproval = ApprovalPolicyResolver.RequiresDeploymentApproval(approvalPolicy);

        // Assert
        requiresApproval.Should().BeFalse();
    }

    [Fact]
    public void RequiresDeploymentApproval_PolicyStatingYes_IsTrue()
    {
        // Arrange
        var approvalPolicy = Policy() with { RequireDeploymentApproval = new IsRequired(true) };

        // Act
        var requiresApproval = ApprovalPolicyResolver.RequiresDeploymentApproval(approvalPolicy);

        // Assert
        requiresApproval.Should().BeTrue();
    }

    [Fact]
    public void RequiresCaseApproval_NoPolicy_IsFalse()
    {
        // Act
        var requiresApproval = ApprovalPolicyResolver.RequiresCaseApproval(
            null, RuleSeverity.Critical, Payload(1_000_000m), [], Now);

        // Assert
        requiresApproval.Should().BeFalse();
    }

    [Fact]
    public void RequiresCaseApproval_PolicyWithNeitherCondition_IsFalse()
    {
        // Arrange
        var approvalPolicy = Policy() with { RequireDeploymentApproval = new IsRequired(true) };

        // Act
        var requiresApproval = ApprovalPolicyResolver.RequiresCaseApproval(
            approvalPolicy, RuleSeverity.Critical, Payload(1_000_000m), [], Now);

        // Assert
        requiresApproval.Should().BeFalse();
    }

    [Theory]
    [InlineData(RuleSeverity.Info, false)]
    [InlineData(RuleSeverity.Low, false)]
    [InlineData(RuleSeverity.Medium, false)]
    [InlineData(RuleSeverity.High, true)]
    [InlineData(RuleSeverity.Critical, true)]
    public void RequiresCaseApproval_MinimumSeverity_IsAFloorAndNotAnEquality(RuleSeverity severity, bool expected)
    {
        // Arrange
        var approvalPolicy = Policy() with { CaseDecisionMinimumSeverity = RuleSeverity.High };

        // Act
        var requiresApproval = ApprovalPolicyResolver.RequiresCaseApproval(
            approvalPolicy, severity, Payload(1m), [], Now);

        // Assert
        requiresApproval.Should().Be(expected);
    }

    [Fact]
    public void RequiresCaseApproval_Matcher_GatesOnTheAmount()
    {
        // Arrange
        var approvalPolicy = Policy() with
        {
            CaseDecisionMatcher = Leaf("payment.amount", RuleCondition.GreaterThan, 10_000m)
        };

        // Act
        var large = ApprovalPolicyResolver.RequiresCaseApproval(approvalPolicy, RuleSeverity.Info, Payload(50_000m), [], Now);
        var small = ApprovalPolicyResolver.RequiresCaseApproval(approvalPolicy, RuleSeverity.Info, Payload(500m), [], Now);

        // Assert
        large.Should().BeTrue();
        small.Should().BeFalse();
    }

    [Fact]
    public void RequiresCaseApproval_BothConditionsDeclared_EitherOneIsEnough()
    {
        // Arrange
        var approvalPolicy = Policy() with
        {
            CaseDecisionMinimumSeverity = RuleSeverity.Critical,
            CaseDecisionMatcher = Leaf("payment.amount", RuleCondition.GreaterThan, 10_000m)
        };

        // Act
        var severityOnly = ApprovalPolicyResolver.RequiresCaseApproval(approvalPolicy, RuleSeverity.Critical, Payload(1m), [], Now);
        var amountOnly = ApprovalPolicyResolver.RequiresCaseApproval(approvalPolicy, RuleSeverity.Info, Payload(50_000m), [], Now);
        var neither = ApprovalPolicyResolver.RequiresCaseApproval(approvalPolicy, RuleSeverity.Info, Payload(1m), [], Now);

        // Assert
        severityOnly.Should().BeTrue();
        amountOnly.Should().BeTrue();
        neither.Should().BeFalse();
    }

    [Fact]
    public void RequiresCaseApproval_Matcher_ReadsThePartiesItIsGiven()
    {
        // Arrange
        var approvalPolicy = Policy() with
        {
            CaseDecisionMatcher = new RuleBody
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
        var risky = ApprovalPolicyResolver.RequiresCaseApproval(approvalPolicy, RuleSeverity.Info, Payload(1m), [Party(90)], Now);
        var safe = ApprovalPolicyResolver.RequiresCaseApproval(approvalPolicy, RuleSeverity.Info, Payload(1m), [Party(10)], Now);

        // Assert
        risky.Should().BeTrue();
        safe.Should().BeFalse();
    }

    [Fact]
    public void DeclaresPayloadCondition_IsTrueOnlyWhereAMatcherWasStated()
    {
        // Arrange
        var severityOnly = Policy() with { CaseDecisionMinimumSeverity = RuleSeverity.High };
        var withMatcher = Policy() with { CaseDecisionMatcher = Leaf("payment.amount", RuleCondition.GreaterThan, 1m) };

        // Act
        ApprovalPolicyResolver.DeclaresPayloadCondition(null).Should().BeFalse();
        ApprovalPolicyResolver.DeclaresPayloadCondition(severityOnly).Should().BeFalse();
        ApprovalPolicyResolver.DeclaresPayloadCondition(withMatcher).Should().BeTrue();
    }

    private ApprovalPolicy Policy()
    {
        return FakeApprovalPolicy.CreateValid(Fixture);
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
