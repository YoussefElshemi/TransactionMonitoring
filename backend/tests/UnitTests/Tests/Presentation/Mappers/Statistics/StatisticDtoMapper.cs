using Core.Constants;
using FluentAssertions;
using Presentation.Mappers.Statistics;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Statistics;

namespace UnitTests.Tests.Presentation.Mappers.Statistics;

public class StatisticDtoMapperTests : BaseTestClass
{
    [Fact]
    public void ToDto_Statistic()
    {
        // Arrange
        var statistic = FakeStatistic.CreateValid(Fixture);

        // Act
        var statisticResponseDto = statistic.ToDto();

        // Assert
        statisticResponseDto.Case.Total.Should().Be(statistic.Case.Total);
        statisticResponseDto.Case.ByStatus.Should().BeEquivalentTo(
            statistic.Case.ByStatus.ToDictionary(x => x.Key.ToString(), x => (int)x.Value));
        statisticResponseDto.Case.AverageResolutionTime.Should().Be(statistic.Case.AverageResolutionTime?.TotalMilliseconds);
        statisticResponseDto.Usage.Should().BeEquivalentTo(statistic.Usage.ToDictionary(x => x.Key.ToString(DateTimeConstants.DateFormat), x => x.Value));
    }
}