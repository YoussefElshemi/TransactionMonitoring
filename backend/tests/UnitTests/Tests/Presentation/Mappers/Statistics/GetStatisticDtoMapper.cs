using Core.ValueObjects;
using FluentAssertions;
using Presentation.Mappers.Statistics;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.Statistics;

namespace UnitTests.Tests.Presentation.Mappers.Statistics;

public class GetStatisticDtoMapperTests : BaseTestClass
{
    [Fact]
    public void ToModel_StatisticRequestDto()
    {
        // Arrange
        var statisticRequestDto = FakeStatisticRequestDto.CreateValid(Fixture);

        // Act
        var statisticRequest = statisticRequestDto.ToModel();

        // Assert
        statisticRequest.OrganizationId.Should().Be(new OrganizationId(statisticRequestDto.OrganizationId));
        statisticRequest.FromDate.Should().Be(new DateRange(statisticRequestDto.FromDate));
        statisticRequest.ToDate.Should().Be(new DateRange(statisticRequestDto.ToDate));
    }
}