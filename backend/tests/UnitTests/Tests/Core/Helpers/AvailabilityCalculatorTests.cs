using Core.Constants;
using Core.Enums;
using Core.Helpers;
using Core.Models.Availability;
using Core.Models.Status;
using Core.ValueObjects;
using FluentAssertions;
using UnitTests.TestHelpers;

namespace UnitTests.Tests.Core.Helpers;

public class AvailabilityCalculatorTests : BaseTestClass
{
    private static readonly DateTimeOffset PeriodStartAt = new(2026, 5, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset PeriodEndAt = new(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CalculateUptimePercentage_GivenNoFailures_IsOneHundredPerCent()
    {
        // Arrange
        const long total = 10_000;

        // Act
        var uptime = AvailabilityCalculator.CalculateUptimePercentage(total, 0);

        // Assert
        ((decimal)uptime).Should().Be(100m);
    }

    [Fact]
    public void CalculateUptimePercentage_GivenOneFailureInTenThousand_KeepsFourDecimalPlaces()
    {
        // Arrange
        const long total = 10_000;

        // Act
        var uptime = AvailabilityCalculator.CalculateUptimePercentage(total, 1);

        // Assert
        ((decimal)uptime).Should().Be(99.99m);
    }

    [Fact]
    public void CalculateUptimePercentage_GivenAFigureBelowTheLastDecimalPlace_RoundsAgainstThePlatform()
    {
        // Act
        var uptime = AvailabilityCalculator.CalculateUptimePercentage(3, 1);

        // Assert
        ((decimal)uptime).Should().Be(66.6666m);
    }

    [Fact]
    public void CalculateUptimePercentage_GivenNoTrafficAtAll_IsOneHundredPerCent()
    {
        // Act
        var uptime = AvailabilityCalculator.CalculateUptimePercentage(0, 0);

        // Assert
        ((decimal)uptime).Should().Be(100m);
    }

    [Fact]
    public void Measure_GivenTrafficAndNoCommitment_ReportsTheFiguresAndNoVerdict()
    {
        // Arrange
        var windows = new List<AvailabilityWindow> { Window(PeriodStartAt, 1000, 10, 50_000, 900) };

        // Act
        var measurement = Measure(windows, [], null);

        // Assert
        ((int)measurement.TotalRequests).Should().Be(1000);
        ((int)measurement.FailedRequests).Should().Be(10);
        ((decimal)measurement.UptimePercentage).Should().Be(99m);
        ((int)measurement.MeanLatencyMilliseconds).Should().Be(50);
        measurement.AvailabilityTarget.Should().BeNull();
        measurement.MetAvailabilityTarget.Should().BeNull();
        measurement.MetLatencyTarget.Should().BeNull();
    }

    [Fact]
    public void Measure_GivenACommitmentTheMonthMet_SaysSo()
    {
        // Arrange
        var windows = new List<AvailabilityWindow> { Window(PeriodStartAt, 10_000, 1, 500_000, 400) };

        // Act
        var measurement = Measure(windows, [], Commitment(99.9m, 1000, 99));

        // Assert
        ((bool)measurement.MetAvailabilityTarget!.Value).Should().BeTrue();
        ((bool)measurement.MetLatencyTarget!.Value).Should().BeTrue();
    }

    [Fact]
    public void Measure_GivenACommitmentTheMonthMissed_SaysSo()
    {
        // Arrange
        var windows = new List<AvailabilityWindow> { Window(PeriodStartAt, 1000, 50, 50_000, 900) };

        // Act
        var measurement = Measure(windows, [], Commitment(99.9m, 1000, 99));

        // Assert
        ((decimal)measurement.UptimePercentage).Should().Be(95m);
        ((bool)measurement.MetAvailabilityTarget!.Value).Should().BeFalse();
    }

    [Fact]
    public void Measure_GivenAnHourFullyCoveredByAnnouncedMaintenance_ExcludesIt()
    {
        // Arrange
        var windows = new List<AvailabilityWindow>
        {
            Window(PeriodStartAt, 100, 100, 5_000, 900),
            Window(PeriodStartAt.AddHours(1), 900, 0, 45_000, 900)
        };

        var maintenance = new List<MaintenanceWindow> { Maintenance(PeriodStartAt, PeriodStartAt.AddHours(1)) };

        // Act
        var measurement = Measure(windows, maintenance, null);

        // Assert
        ((int)measurement.TotalRequests).Should().Be(900);
        ((int)measurement.FailedRequests).Should().Be(0);
        ((decimal)measurement.UptimePercentage).Should().Be(100m);
    }

    [Fact]
    public void Measure_GivenAnHourOnlyPartlyCoveredByMaintenance_CountsItInFull()
    {
        // Arrange
        var windows = new List<AvailabilityWindow> { Window(PeriodStartAt, 100, 100, 5_000, 900) };

        var maintenance = new List<MaintenanceWindow>
        {
            Maintenance(PeriodStartAt.AddMinutes(10), PeriodStartAt.AddMinutes(20))
        };

        // Act
        var measurement = Measure(windows, maintenance, null);

        // Assert
        ((int)measurement.TotalRequests).Should().Be(100);
        ((decimal)measurement.UptimePercentage).Should().Be(0m);
    }

    [Fact]
    public void CalculatePercentileLatency_GivenTenMillisecondBuckets_ReturnsTheNearestRankUpperBound()
    {
        // Arrange
        var window = WindowWithBuckets(Enumerable.Range(0, 10).ToDictionary(x => x * 10 + 9, _ => 10));

        // Act
        var latency = AvailabilityCalculator.CalculatePercentileLatency([window], new LatencyPercentile(95), 99);

        // Assert
        latency.Should().Be(99);
    }

    [Fact]
    public void CalculatePercentileLatency_GivenThePercentileFallsInTheOpenTopBucket_ReturnsTheObservedMaximum()
    {
        // Arrange
        var window = WindowWithBuckets(new Dictionary<int, int> { [50] = 90, [int.MaxValue] = 10 });

        // Act
        var latency = AvailabilityCalculator.CalculatePercentileLatency([window], new LatencyPercentile(99), 61_000);

        // Assert
        latency.Should().Be(61_000);
    }

    [Fact]
    public void CalculatePercentileLatency_GivenNothingMeasured_IsZero()
    {
        // Act
        var latency = AvailabilityCalculator.CalculatePercentileLatency([], new LatencyPercentile(99), 0);

        // Assert
        latency.Should().Be(0);
    }

    private static AvailabilityMeasurement Measure(
        List<AvailabilityWindow> windows,
        List<MaintenanceWindow> maintenance,
        AvailabilityCommitment? commitment)
    {
        return AvailabilityCalculator.Measure(
            new OrganizationId(Guid.NewGuid()),
            new PeriodStart(PeriodStartAt),
            new PeriodEnd(PeriodEndAt),
            windows,
            maintenance,
            commitment);
    }

    private static AvailabilityWindow Window(DateTimeOffset periodStart, int total, int failed, long totalLatency, int maxLatency)
    {
        return new AvailabilityWindow
        {
            AvailabilityWindowId = new AvailabilityWindowId(Guid.NewGuid()),
            OrganizationId = new OrganizationId(Guid.NewGuid()),
            PeriodStart = new PeriodStart(periodStart),
            PeriodEnd = new PeriodEnd(periodStart.AddMinutes(AvailabilityConstants.WindowMinutes)),
            TotalRequests = new RequestCount(total),
            FailedRequests = new RequestCount(failed),
            TotalLatencyMilliseconds = new TotalLatencyMilliseconds(totalLatency),
            MaxLatencyMilliseconds = new LatencyMilliseconds(maxLatency),
            LatencyBuckets = [],
            CreatedAt = new CreatedAt(periodStart),
            UpdatedAt = new UpdatedAt(periodStart)
        };
    }

    private static AvailabilityWindow WindowWithBuckets(Dictionary<int, int> buckets)
    {
        var window = Window(PeriodStartAt, buckets.Values.Sum(), 0, 0, 0);

        return window with
        {
            LatencyBuckets = buckets
                .Select(x => new AvailabilityLatencyBucket
                {
                    AvailabilityWindowId = window.AvailabilityWindowId,
                    UpperBoundMilliseconds = new LatencyBucketBound(x.Key),
                    Count = new RequestCount(x.Value),
                    CreatedAt = new CreatedAt(PeriodStartAt),
                    UpdatedAt = new UpdatedAt(PeriodStartAt)
                })
                .ToList()
        };
    }

    private static AvailabilityCommitment Commitment(decimal target, int latencyTarget, int percentile)
    {
        return new AvailabilityCommitment
        {
            AvailabilityCommitmentId = new AvailabilityCommitmentId(Guid.NewGuid()),
            OrganizationId = new OrganizationId(Guid.NewGuid()),
            AvailabilityTarget = new AvailabilityTarget(target),
            LatencyTarget = new LatencyTarget(latencyTarget),
            LatencyPercentile = new LatencyPercentile(percentile),
            EffectiveFrom = new EffectiveFrom(PeriodStartAt),
            EffectiveTo = null,
            CreditBands = [],
            CreatedByUserId = null,
            CreatedAt = new CreatedAt(PeriodStartAt),
            UpdatedAt = new UpdatedAt(PeriodStartAt),
            IsDeleted = new IsDeleted(false),
            DeletedAt = null
        };
    }

    private static MaintenanceWindow Maintenance(DateTimeOffset start, DateTimeOffset end)
    {
        return new MaintenanceWindow
        {
            MaintenanceWindowId = new MaintenanceWindowId(Guid.NewGuid()),
            Title = new MaintenanceWindowTitle("Database upgrade"),
            Description = new MaintenanceWindowDescription("Rolling restart of the primary."),
            Status = MaintenanceStatus.Completed,
            Components = [ServiceComponent.Api],
            ScheduledStart = new ScheduledStart(start),
            ScheduledEnd = new ScheduledEnd(end),
            AnnouncedAt = new AnnouncedAt(start.AddDays(-7)),
            CreatedByUserId = null,
            CreatedAt = new CreatedAt(start.AddDays(-7)),
            UpdatedAt = new UpdatedAt(start.AddDays(-7)),
            IsDeleted = new IsDeleted(false),
            DeletedAt = null
        };
    }
}
