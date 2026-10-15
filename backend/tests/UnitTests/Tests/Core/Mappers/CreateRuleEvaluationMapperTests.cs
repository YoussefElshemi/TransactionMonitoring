using Core.Enums;
using Core.Mappers;
using Core.ValueObjects;
using FluentAssertions;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.RuleEvaluations;

namespace UnitTests.Tests.Core.Mappers;

public class CreateRuleEvaluationMapperTests : BaseTestClass
{
    [Fact]
    public void ToModel_CreateRuleEvaluationRequest()
    {
        // Arrange
        var createRuleEvaluationRequest = FakeCreateRuleEvaluationRequest.CreateValid(Fixture);

        // Act
        var ruleEvaluation = createRuleEvaluationRequest.ToModel(MockTimeProvider, Mode.Live);

        // Assert
        ruleEvaluation.RuleEvaluationId.Should().NotBeNull();
        ruleEvaluation.OrganizationId.Should().Be(createRuleEvaluationRequest.OrganizationId);
        ruleEvaluation.RuleId.Should().Be(createRuleEvaluationRequest.RuleId);
        ruleEvaluation.TransactionId.Should().Be(createRuleEvaluationRequest.TransactionId);
        ruleEvaluation.Matched.Should().Be(createRuleEvaluationRequest.Matched);
        ruleEvaluation.Rule.Should().BeNull();
        ruleEvaluation.IsDeleted.Should().Be(new IsDeleted(false));
        ruleEvaluation.DeletedAt.Should().BeNull();
    }
}
