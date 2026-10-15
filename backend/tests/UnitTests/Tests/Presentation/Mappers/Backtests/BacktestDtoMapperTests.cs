using FluentAssertions;
using Presentation.Mappers.BackTests;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.BackTests;

namespace UnitTests.Tests.Presentation.Mappers.BackTests;

public class BackTestDtoMapperTests : BaseTestClass
{
    [Fact]
    public void ToDto_BackTest()
    {
        // Arrange
        var backTest = FakeBackTest.CreateValid(Fixture);

        // Act
        var dto = backTest.ToDto();

        // Assert
        dto.BackTestId.Should().Be(backTest.BackTestId);
        dto.Status.Should().Be(backTest.Status.ToString());
        dto.SampleSize.Should().Be(backTest.SampleSize);
        dto.TotalEvaluations.Should().Be(backTest.TotalEvaluations);
        dto.ProcessedCount.Should().Be(backTest.ProcessedCount);
        dto.CreatedAt.Should().Be(backTest.CreatedAt);
        dto.UpdatedAt.Should().Be(backTest.UpdatedAt);
        dto.CompletedAt.Should().Be(backTest.CompletedAt!.Value);
    }
}
