using FluentAssertions;
using Presentation.Availability;
using UnitTests.TestHelpers;

namespace UnitTests.Tests.Presentation.Availability;

public class AvailabilityRecorderTests : BaseTestClass
{
    private static readonly DateTimeOffset ObservedAt = new(2026, 5, 14, 10, 37, 42, TimeSpan.Zero);

    [Fact]
    public void Drain_GivenRecordedRequests_ReportsTheCountsAndTheHour()
    {
        // Arrange
        var recorder = new AvailabilityRecorder();
        var organizationId = Guid.NewGuid();

        // Act
        recorder.Record(organizationId, ObservedAt, 40, false);
        recorder.Record(organizationId, ObservedAt, 60, false);
        recorder.Record(organizationId, ObservedAt, 500, true);

        var drained = recorder.Drain();

        // Assert
        drained.Should().ContainSingle();
        ((Guid)drained[0].OrganizationId).Should().Be(organizationId);
        ((DateTimeOffset)drained[0].PeriodStart).Should().Be(new DateTimeOffset(2026, 5, 14, 10, 0, 0, TimeSpan.Zero));
        ((int)drained[0].TotalRequests).Should().Be(3);
        ((int)drained[0].FailedRequests).Should().Be(1);
        ((long)drained[0].TotalLatencyMilliseconds).Should().Be(600);
        ((int)drained[0].MaxLatencyMilliseconds).Should().Be(500);
    }

    [Fact]
    public void Drain_GivenRequestsAcrossLatencyRanges_GroupsThemIntoTenMillisecondBuckets()
    {
        // Arrange
        var recorder = new AvailabilityRecorder();
        var organizationId = Guid.NewGuid();

        // Act
        recorder.Record(organizationId, ObservedAt, 0, false);
        recorder.Record(organizationId, ObservedAt, 9, false);
        recorder.Record(organizationId, ObservedAt, 10, false);
        recorder.Record(organizationId, ObservedAt, 19, false);
        recorder.Record(organizationId, ObservedAt, 20, false);

        var drained = recorder.Drain();

        // Assert
        var buckets = drained[0].LatencyBuckets.ToDictionary(x => (int)x.UpperBoundMilliseconds, x => (int)x.Count);
        buckets[9].Should().Be(2);
        buckets[19].Should().Be(2);
        buckets[29].Should().Be(1);
    }

    [Fact]
    public void Drain_GivenTheMaximumLatency_DoesNotOverflowTheBucketBound()
    {
        // Arrange
        var recorder = new AvailabilityRecorder();
        var organizationId = Guid.NewGuid();

        // Act
        recorder.Record(organizationId, ObservedAt, int.MaxValue, false);

        var drained = recorder.Drain();

        // Assert
        var buckets = drained[0].LatencyBuckets.ToDictionary(x => (int)x.UpperBoundMilliseconds, x => (int)x.Count);
        buckets[int.MaxValue].Should().Be(1);
    }

    [Fact]
    public void Drain_GivenRequestsInDifferentHours_KeepsThemApart()
    {
        // Arrange
        var recorder = new AvailabilityRecorder();
        var organizationId = Guid.NewGuid();

        // Act
        recorder.Record(organizationId, ObservedAt, 10, false);
        recorder.Record(organizationId, ObservedAt.AddHours(1), 10, false);

        var drained = recorder.Drain();

        // Assert
        drained.Should().HaveCount(2);
    }

    [Fact]
    public void Drain_CalledTwice_ReturnsNothingTheSecondTime()
    {
        // Arrange
        var recorder = new AvailabilityRecorder();
        recorder.Record(Guid.NewGuid(), ObservedAt, 10, false);

        // Act
        var first = recorder.Drain();
        var second = recorder.Drain();

        // Assert
        first.Should().ContainSingle();
        second.Should().BeEmpty();
    }

    [Fact]
    public void ToPeriodStart_GivenAnyInstant_TruncatesToTheHourInUtc()
    {
        // Act
        var periodStart = AvailabilityRecorder.ToPeriodStart(new DateTimeOffset(2026, 5, 14, 11, 37, 42, TimeSpan.FromHours(2)));

        // Assert
        periodStart.Should().Be(new DateTimeOffset(2026, 5, 14, 9, 0, 0, TimeSpan.Zero));
    }
}
