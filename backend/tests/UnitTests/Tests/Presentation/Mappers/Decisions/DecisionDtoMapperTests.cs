using Core.ValueObjects;
using FluentAssertions;
using Presentation.Mappers.Decisions;
using Presentation.Mappers.RuleEvaluations;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Decisions;
using UnitTests.TestHelpers.FakeObjects.Core.Models.RuleEvaluations;

namespace UnitTests.Tests.Presentation.Mappers.Decisions;

public class DecisionDtoMapperTests : BaseTestClass
{
    [Fact]
    public void ToDto_Decision()
    {
        // Arrange
        var decision = FakeDecision.CreateValid(Fixture);

        // Act
        var decisionDto = decision.ToDto();

        // Assert
        decisionDto.DecisionId.Should().Be(decision.DecisionId);
        decisionDto.TransactionId.Should().Be(decision.TransactionId);
        decisionDto.TransactionReference.Should().Be(decision.ExternalReference!.Value.ToString());
        decisionDto.Status.Should().Be(decision.Status.ToString());
        decisionDto.ReasonCode.Should().Be(decision.ReasonCode.ToString());
        decisionDto.CaseId.Should().Be(decision.CaseId);
        decisionDto.HeldAt.Should().Be(decision.HeldAt);
        decisionDto.ResolvedAt.Should().Be(decision.ResolvedAt);
        decisionDto.CreatedAt.Should().Be(decision.CreatedAt);
        decisionDto.UpdatedAt.Should().Be(decision.UpdatedAt);
    }

    [Fact]
    public void ToDto_Decision_ReportsOnlyTheRulesThatFired()
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
        var decisionDto = decision.ToDto();

        // Assert
        decisionDto.FiredRules.Should().BeEquivalentTo([matchedEvaluation.ToDto()]);
    }

    [Fact]
    public void ToDto_Decision_WithoutAnExternalReferenceOrResolvingUser()
    {
        // Arrange
        var decision = FakeDecision.CreateValid(Fixture) with
        {
            ExternalReference = null,
            ResolvedByUser = null,
            CaseId = null
        };

        // Act
        var decisionDto = decision.ToDto();

        // Assert
        decisionDto.TransactionReference.Should().BeNull();
        decisionDto.CaseId.Should().BeNull();
        decisionDto.FiredRules.Should().BeEmpty();
    }
}
