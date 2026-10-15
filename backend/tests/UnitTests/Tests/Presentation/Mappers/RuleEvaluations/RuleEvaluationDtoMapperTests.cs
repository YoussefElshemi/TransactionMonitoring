using FluentAssertions;
using Presentation.Mappers.RuleEvaluations;
using Presentation.Mappers.Rules;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.RuleEvaluations;

namespace UnitTests.Tests.Presentation.Mappers.RuleEvaluations;

public class RuleEvaluationDtoMapperTests : BaseTestClass
{
    [Fact]
    public void ToDto_RuleEvaluation()
    {
        // Arrange
        var ruleEvaluation = FakeRuleEvaluation.CreateValid(Fixture);

        // Act
        var dto = ruleEvaluation.ToDto();

        // Assert
        dto.RuleEvaluationId.Should().Be(ruleEvaluation.RuleEvaluationId);
        dto.RuleVersion.Should().BeEquivalentTo(ruleEvaluation.RuleVersion!.ToDto());
        dto.TransactionId.Should().Be(ruleEvaluation.TransactionId);
        dto.Matched.Should().Be(ruleEvaluation.Matched);
        dto.CreatedAt.Should().Be(ruleEvaluation.CreatedAt);
        dto.UpdatedAt.Should().Be(ruleEvaluation.UpdatedAt);

        var conditionMatch = ruleEvaluation.ConditionMatches!.Single();
        var conditionMatchDto = dto.ConditionMatches.Should().ContainSingle().Subject;
        conditionMatchDto.Field.Should().Be(conditionMatch.Field);
        conditionMatchDto.Condition.Should().Be(conditionMatch.Condition.ToString());
        conditionMatchDto.Expected.Should().BeSameAs(conditionMatch.Expected);
        conditionMatchDto.Actual.Should().BeSameAs(conditionMatch.Actual);
        conditionMatchDto.Matched.Should().Be(conditionMatch.Matched);
    }

    [Fact]
    public void ToDto_RuleEvaluation_WithoutConditionMatches()
    {
        // Arrange
        var ruleEvaluation = FakeRuleEvaluation.CreateValid(Fixture) with
        {
            ConditionMatches = null
        };

        // Act
        var dto = ruleEvaluation.ToDto();

        // Assert
        dto.ConditionMatches.Should().BeNull();
    }
}
