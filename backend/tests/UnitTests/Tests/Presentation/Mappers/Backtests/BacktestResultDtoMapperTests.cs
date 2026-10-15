using FluentAssertions;
using Presentation.Mappers.BackTests;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.BackTests;

namespace UnitTests.Tests.Presentation.Mappers.BackTests;

public class BackTestResultDtoMapperTests : BaseTestClass
{
    [Fact]
    public void ToDto_BackTestResult()
    {
        // Arrange
        var backTestResult = FakeBackTestResult.CreateValid(Fixture);

        // Act
        var dto = backTestResult.ToDto();

        // Assert
        dto.BackTestResultId.Should().Be(backTestResult.BackTestResultId);
        dto.RuleEvaluationId.Should().Be(backTestResult.RuleEvaluationId);
        dto.OriginalMatched.Should().Be(backTestResult.OriginalMatched);
        dto.NewMatched.Should().Be(backTestResult.NewMatched);
        dto.HasChanged.Should().Be(backTestResult.HasChanged);
        dto.CreatedAt.Should().Be(backTestResult.CreatedAt);
    }
}
