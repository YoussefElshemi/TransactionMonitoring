using Core.Enums;
using Core.Interfaces.Services;
using Core.Models.Cases;
using Core.Models.Statistics;
using Core.Services;
using Core.ValueObjects;
using FluentAssertions;
using Moq;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Statistics;

namespace UnitTests.Tests.Core.Services;

public class StatisticsServiceTests : BaseTestClass
{
    private readonly Mock<ICaseService> _mockCaseService = new();
    private readonly Mock<IUsageService> _mockUsageService = new();
    private readonly StatisticsService _sut;

    public StatisticsServiceTests()
    {
        _sut = new StatisticsService(
            _mockCaseService.Object,
            _mockUsageService.Object);
    }

    [Fact]
    public async Task GetStatisticsAsync_GivenStatisticRequest_ReturnsStatistics()
    {
        // Arrange
        var statisticsRequest = FakeStatisticsRequest.CreateValid(Fixture);
        var usage = new Dictionary<DateOnly, int>
        {
            { DateOnly.FromDateTime(MockTimeProvider.GetUtcNow().UtcDateTime), 100 }
        };

        _mockCaseService
            .Setup(x => x.CountByStatusAsync(It.IsAny<ListCaseRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enum.GetValues<CaseStatus>().ToDictionary(x => x, _ => new TotalCount(2)));

        _mockCaseService
            .Setup(x => x.GetAverageResolutionTimeAsync(It.IsAny<StatisticRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TimeSpan(0, 0, 10));

        _mockCaseService
            .Setup(x => x.CountBreachedAsync(It.IsAny<ListCaseRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TotalCount(3));

        _mockUsageService
            .Setup(x => x.GroupByDateAsync(It.IsAny<StatisticRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(usage);

        // Act
        var statistics = await _sut.GetStatisticsAsync(statisticsRequest, CancellationToken.None);

        // Assert
        var caseStatuses = Enum.GetValues<CaseStatus>();

        statistics.Case.ByStatus.Should().HaveCount(caseStatuses.Length);
        statistics.Case.ByStatus.Should().OnlyContain(x => x.Value == new TotalCount(2));
        statistics.Case.Total.Should().Be(new TotalCount(2 * caseStatuses.Length));
        statistics.Case.AverageResolutionTime.Should().Be(new TimeSpan(0, 0, 10));

        statistics.Case.Breached.Should().Be(new TotalCount(3));
        ((decimal)statistics.Case.BreachRate).Should().Be(16.67m);
        statistics.Usage.Should().BeEquivalentTo(usage);

        _mockCaseService.Verify(x => x.CountByStatusAsync(It.IsAny<ListCaseRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockCaseService.Verify(x => x.GetAverageResolutionTimeAsync(It.IsAny<StatisticRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUsageService.Verify(x => x.GroupByDateAsync(It.IsAny<StatisticRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}