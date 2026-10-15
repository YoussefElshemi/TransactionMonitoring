using FluentAssertions;
using Presentation.Mappers.Cases;
using Presentation.Mappers.Decisions;
using Presentation.Mappers.Rules;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Rules;

namespace UnitTests.Tests.Presentation.Mappers.Rules;

public class EvaluateRuleDtoMapperTests : BaseTestClass
{
    [Fact]
    public void ToDto_EvaluateRuleResponse()
    {
        // Arrange
        var evaluateRuleResponse = FakeEvaluateRuleResponse.CreateValid(Fixture);

        // Act
        var evaluateAllRulesResponseDto = evaluateRuleResponse.ToDto();

        // Assert
        evaluateAllRulesResponseDto.FriendlyName.Should().Be(evaluateRuleResponse.FriendlyName);
        evaluateAllRulesResponseDto.Matched.Should().Be(evaluateRuleResponse.Matched);
        evaluateAllRulesResponseDto.TransactionId.Should().Be(evaluateRuleResponse.TransactionId);
        evaluateAllRulesResponseDto.TransactionReference.Should().Be(evaluateRuleResponse.ExternalReference!.Value.ToString());
        evaluateAllRulesResponseDto.Case.Should().Be(evaluateRuleResponse.Case?.ToDto());
        evaluateAllRulesResponseDto.Decision.Should().BeEquivalentTo(evaluateRuleResponse.Decision!.ToDto());
    }

    [Fact]
    public void ToDto_EvaluateRuleResponse_WithoutAnExternalReference()
    {
        var evaluateRuleResponse = FakeEvaluateRuleResponse.CreateValid(Fixture) with
        {
            ExternalReference = null
        };

        var evaluateAllRulesResponseDto = evaluateRuleResponse.ToDto();

        evaluateAllRulesResponseDto.TransactionId.Should().Be(evaluateRuleResponse.TransactionId);
        evaluateAllRulesResponseDto.TransactionReference.Should().BeNull();
    }

    [Fact]
    public void ToNestedDto_EvaluateRuleResponse_OmitsTheTransaction()
    {
        var evaluateRuleResponse = FakeEvaluateRuleResponse.CreateValid(Fixture);

        var evaluateAllRulesResponseDto = evaluateRuleResponse.ToNestedDto();

        evaluateAllRulesResponseDto.Matched.Should().Be(evaluateRuleResponse.Matched);
        evaluateAllRulesResponseDto.TransactionId.Should().BeNull();
        evaluateAllRulesResponseDto.TransactionReference.Should().BeNull();
    }

    [Fact]
    public void ToNestedDto_EvaluateRuleResponse_OmitsTheDecision()
    {
        // Arrange
        var evaluateRuleResponse = FakeEvaluateRuleResponse.CreateValid(Fixture);

        // Act
        var evaluateAllRulesResponseDto = evaluateRuleResponse.ToNestedDto();

        // Assert
        evaluateRuleResponse.Decision.Should().NotBeNull();
        evaluateAllRulesResponseDto.Decision.Should().BeNull();
    }

    [Fact]
    public void ToDto_EvaluateRuleResponse_WithoutADecision()
    {
        // Arrange
        var evaluateRuleResponse = FakeEvaluateRuleResponse.CreateValid(Fixture) with
        {
            Decision = null
        };

        // Act
        var evaluateAllRulesResponseDto = evaluateRuleResponse.ToDto();

        // Assert
        evaluateAllRulesResponseDto.Decision.Should().BeNull();
    }
}