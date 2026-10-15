using FluentAssertions;
using Presentation.Mappers.Cases;
using Presentation.Mappers.Decisions;
using Presentation.Mappers.Rules;
using Presentation.Mappers.RuleSets;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.RuleSets;

namespace UnitTests.Tests.Presentation.Mappers.RuleSets;

public class EvaluateRuleSetResponseMapperTests : BaseTestClass
{
    [Fact]
    public void ToDto_EvaluateRuleSetResponse()
    {
        // Arrange
        var evaluateRuleSetResponse = FakeEvaluateRuleSetResponse.CreateValid(Fixture);

        // Act
        var evaluateRuleSetResponseDto = evaluateRuleSetResponse.ToDto();

        // Assert
        evaluateRuleSetResponseDto.FriendlyName.Should().Be(evaluateRuleSetResponse.FriendlyName);
        evaluateRuleSetResponseDto.Results.Should().BeEquivalentTo(evaluateRuleSetResponse.Results.Select(x => x.ToNestedDto()).ToArray());
        evaluateRuleSetResponseDto.TransactionId.Should().Be(evaluateRuleSetResponse.TransactionId);
        evaluateRuleSetResponseDto.TransactionReference.Should().Be(evaluateRuleSetResponse.ExternalReference!.Value.ToString());
        evaluateRuleSetResponseDto.Case.Should().BeEquivalentTo(evaluateRuleSetResponse.Case?.ToDto());
        evaluateRuleSetResponseDto.Decision.Should().BeEquivalentTo(evaluateRuleSetResponse.Decision!.ToDto());
    }

    [Fact]
    public void ToDto_EvaluateRuleSetResponse_LeavesTheTransactionOffTheNestedResults()
    {
        var evaluateRuleSetResponse = FakeEvaluateRuleSetResponse.CreateValid(Fixture);

        var evaluateRuleSetResponseDto = evaluateRuleSetResponse.ToDto();

        evaluateRuleSetResponseDto.Results.Should().OnlyContain(x => x.TransactionId == null && x.TransactionReference == null);
    }

    [Fact]
    public void ToDto_EvaluateRuleSetResponse_LeavesTheDecisionOffTheNestedResults()
    {
        // Arrange
        var evaluateRuleSetResponse = FakeEvaluateRuleSetResponse.CreateValid(Fixture);

        // Act
        var evaluateRuleSetResponseDto = evaluateRuleSetResponse.ToDto();

        // Assert
        evaluateRuleSetResponseDto.Decision.Should().NotBeNull();
        evaluateRuleSetResponseDto.Results.Should().OnlyContain(x => x.Decision == null);
    }

    [Fact]
    public void ToDto_EvaluateRuleSetResponse_WithoutADecision()
    {
        // Arrange
        var evaluateRuleSetResponse = FakeEvaluateRuleSetResponse.CreateValid(Fixture) with
        {
            Decision = null
        };

        // Act
        var evaluateRuleSetResponseDto = evaluateRuleSetResponse.ToDto();

        // Assert
        evaluateRuleSetResponseDto.Decision.Should().BeNull();
    }
}