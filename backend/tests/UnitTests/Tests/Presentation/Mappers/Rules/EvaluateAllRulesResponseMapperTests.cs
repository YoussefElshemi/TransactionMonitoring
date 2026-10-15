using FluentAssertions;
using Presentation.Mappers.Cases;
using Presentation.Mappers.Decisions;
using Presentation.Mappers.Rules;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Rules;

namespace UnitTests.Tests.Presentation.Mappers.Rules;

public class EvaluateAllRulesResponseMapperTests : BaseTestClass
{
    [Fact]
    public void ToDto_EvaluateAllRulesResponse()
    {
        // Arrange
        var evaluateAllRulesResponse = FakeEvaluateAllRulesResponse.CreateValid(Fixture);

        // Act
        var evaluateAllRulesResponseDto = evaluateAllRulesResponse.ToDto();

        // Assert
        evaluateAllRulesResponseDto.Case.Should().Be(evaluateAllRulesResponse.Case!.ToDto());
        evaluateAllRulesResponseDto.Results.Should().BeEquivalentTo(evaluateAllRulesResponse.Results.Select(x => x.ToNestedDto()).ToArray());
        evaluateAllRulesResponseDto.TransactionId.Should().Be(evaluateAllRulesResponse.TransactionId);
        evaluateAllRulesResponseDto.TransactionReference.Should().Be(evaluateAllRulesResponse.ExternalReference!.Value.ToString());
        evaluateAllRulesResponseDto.Decision.Should().BeEquivalentTo(evaluateAllRulesResponse.Decision!.ToDto());
    }

    [Fact]
    public void ToDto_EvaluateAllRulesResponse_LeavesTheTransactionOffTheNestedResults()
    {
        var evaluateAllRulesResponse = FakeEvaluateAllRulesResponse.CreateValid(Fixture);

        var evaluateAllRulesResponseDto = evaluateAllRulesResponse.ToDto();

        evaluateAllRulesResponseDto.Results.Should().OnlyContain(x => x.TransactionId == null && x.TransactionReference == null);
    }

    [Fact]
    public void ToDto_EvaluateAllRulesResponse_LeavesTheDecisionOffTheNestedResults()
    {
        // Arrange
        var evaluateAllRulesResponse = FakeEvaluateAllRulesResponse.CreateValid(Fixture);

        // Act
        var evaluateAllRulesResponseDto = evaluateAllRulesResponse.ToDto();

        // Assert
        evaluateAllRulesResponseDto.Decision.Should().NotBeNull();
        evaluateAllRulesResponseDto.Results.Should().OnlyContain(x => x.Decision == null);
    }

    [Fact]
    public void ToDto_EvaluateAllRulesResponse_WithoutADecision()
    {
        // Arrange
        var evaluateAllRulesResponse = FakeEvaluateAllRulesResponse.CreateValid(Fixture) with
        {
            Decision = null
        };

        // Act
        var evaluateAllRulesResponseDto = evaluateAllRulesResponse.ToDto();

        // Assert
        evaluateAllRulesResponseDto.Decision.Should().BeNull();
    }
}