using FluentAssertions;
using Presentation.Mappers.Usages;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Usages;

namespace UnitTests.Tests.Presentation.Mappers.Usages;

public class UsageDtoMapperTests : BaseTestClass
{
    [Fact]
    public void ToDto_Usage()
    {
        // Arrange
        var usage = FakeUsage.CreateValid(Fixture);

        // Act
        var usageResponseDto = usage.ToDto();

        // Assert
        usageResponseDto.UsageId.Should().Be(usage.UsageId);
        usageResponseDto.Resource.Should().Be(usage.Resource);
        usageResponseDto.Count.Should().Be(usage.Count);
        usageResponseDto.CreatedAt.Should().Be(usage.CreatedAt);
    }
}