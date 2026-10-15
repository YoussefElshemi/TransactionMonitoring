using Core.Mappers.Webhooks;
using Core.ValueObjects;
using FluentAssertions;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Decisions;
using UnitTests.TestHelpers.FakeObjects.Core.Models.RuleEvaluations;

namespace UnitTests.Tests.Core.Mappers.Webhooks;

public class DecisionWebhookMessageMapperTests : BaseTestClass
{
    [Fact]
    public void ToWebhook_Decision()
    {
        // Arrange
        var decision = FakeDecision.CreateValid(Fixture);

        // Act
        var decisionWebhookMessage = decision.ToWebhook();

        // Assert
        decisionWebhookMessage.DecisionId.Should().Be(decision.DecisionId);
        decisionWebhookMessage.OrganizationId.Should().Be(decision.OrganizationId);
        decisionWebhookMessage.TransactionId.Should().Be(decision.TransactionId);
        decisionWebhookMessage.ExternalReference.Should().Be(decision.ExternalReference!.Value.ToString());
        decisionWebhookMessage.Verdict.Should().Be(decision.Status.ToString());
        decisionWebhookMessage.ReasonCode.Should().Be(decision.ReasonCode.ToString());
        decisionWebhookMessage.CaseId.Should().Be(decision.CaseId);
        decisionWebhookMessage.ResolvedByUser.Should().BeEquivalentTo(decision.ResolvedByUser!.ToWebhook());
        decisionWebhookMessage.HeldAt.Should().Be(decision.HeldAt);
        decisionWebhookMessage.ResolvedAt.Should().Be(decision.ResolvedAt);
        decisionWebhookMessage.CreatedAt.Should().Be(decision.CreatedAt);
        decisionWebhookMessage.UpdatedAt.Should().Be(decision.UpdatedAt);
    }

    [Fact]
    public void ToWebhook_Decision_ReportsOnlyTheRulesThatFired()
    {
        // Arrange
        var matchedEvaluation = FakeRuleEvaluation.CreateValid(Fixture) with
        {
            Matched = new Matched(true)
        };

        var unmatchedEvaluation = FakeRuleEvaluation.CreateValid(Fixture) with
        {
            Matched = new Matched(false)
        };

        var decision = FakeDecision.CreateValid(Fixture) with
        {
            RuleEvaluations = [matchedEvaluation, unmatchedEvaluation]
        };

        // Act
        var decisionWebhookMessage = decision.ToWebhook();

        // Assert
        decisionWebhookMessage.FiredRules.Should().ContainSingle();

        var firedRule = decisionWebhookMessage.FiredRules.Single();
        firedRule.RuleId.Should().Be(matchedEvaluation.RuleId);
        firedRule.RuleVersionId.Should().Be(matchedEvaluation.RuleVersionId);
        firedRule.FriendlyName.Should().Be(matchedEvaluation.Rule!.FriendlyName);
        firedRule.Action.Should().Be(matchedEvaluation.Rule!.Action.ToString());
        firedRule.Severity.Should().Be(matchedEvaluation.Rule!.Severity.ToString());
        firedRule.RiskWeight.Should().Be(matchedEvaluation.Rule!.RiskWeight);
    }

    [Fact]
    public void ToWebhook_Decision_WithoutAnExternalReferenceOrResolvingUser()
    {
        // Arrange
        var decision = FakeDecision.CreateValid(Fixture) with
        {
            ExternalReference = null,
            ResolvedByUser = null,
            CaseId = null
        };

        // Act
        var decisionWebhookMessage = decision.ToWebhook();

        // Assert
        decisionWebhookMessage.ExternalReference.Should().BeNull();
        decisionWebhookMessage.ResolvedByUser.Should().BeNull();
        decisionWebhookMessage.CaseId.Should().BeNull();
        decisionWebhookMessage.FiredRules.Should().BeEmpty();
    }
}
