using Core.Enums;
using Core.Helpers;
using Core.Models.Status;
using Core.ValueObjects;
using FluentAssertions;
using UnitTests.TestHelpers;

namespace UnitTests.Tests.Core.Helpers;

public class UptimeDayCalculatorTests : BaseTestClass
{
    private static readonly DateOnly Day = new(2026, 5, 14);
    private static readonly DateTimeOffset DayStart = new(2026, 5, 14, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset EndOfDay = new(2026, 5, 15, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Calculate_GivenADayOfHealthyProbes_IsFullyAvailable()
    {
        // Arrange
        var probes = Probes((0, true), (1, true), (2, true));

        // Act
        var day = Calculate(probes, [], EndOfDay);

        // Assert
        ((int)day.TotalProbes).Should().Be(3);
        ((int)day.HealthyProbes).Should().Be(3);
        ((int)day.DowntimeSeconds).Should().Be(0);
        ((decimal)day.UptimePercentage).Should().Be(100m);
    }

    [Fact]
    public void Calculate_GivenOneFailedProbe_CountsTheGapUntilTheNextAsDowntime()
    {
        // Arrange
        var probes = Probes((0, true), (1, false), (2, true));

        // Act
        var day = Calculate(probes, [], EndOfDay);

        // Assert
        ((int)day.DowntimeSeconds).Should().Be(60);
        ((decimal)day.UptimePercentage).Should().Be(66.6666m);
    }

    [Fact]
    public void Calculate_GivenTheLastProbeOfTheDayFailed_CountsTheRestOfTheDay()
    {
        // Arrange
        var probes = Probes((1435, true), (1439, false));

        // Act
        var day = Calculate(probes, [], EndOfDay);

        // Assert
        ((int)day.DowntimeSeconds).Should().Be(60);
    }

    [Fact]
    public void Calculate_GivenADayStillInProgress_CountsOnlyUpToNow()
    {
        // Arrange
        var probes = Probes((600, false));
        var now = DayStart.AddMinutes(605);

        // Act
        var day = Calculate(probes, [], now);

        // Assert
        ((int)day.DowntimeSeconds).Should().Be(300);
    }

    [Fact]
    public void Calculate_GivenProbesInsideAnnouncedMaintenance_ExcludesThem()
    {
        // Arrange
        var probes = Probes((0, false), (1, false), (100, true));
        var maintenance = new List<MaintenanceWindow> { Maintenance(DayStart, DayStart.AddMinutes(60), ServiceComponent.Api) };

        // Act
        var day = Calculate(probes, maintenance, EndOfDay);

        // Assert
        ((int)day.TotalProbes).Should().Be(1);
        ((int)day.HealthyProbes).Should().Be(1);
        ((decimal)day.UptimePercentage).Should().Be(100m);
    }

    [Fact]
    public void Calculate_GivenMaintenanceOverAnotherComponent_CountsTheProbesAnyway()
    {
        // Arrange
        var probes = Probes((0, false), (1, true));
        var maintenance = new List<MaintenanceWindow> { Maintenance(DayStart, DayStart.AddMinutes(60), ServiceComponent.Webhooks) };

        // Act
        var day = Calculate(probes, maintenance, EndOfDay);

        // Assert
        ((int)day.TotalProbes).Should().Be(2);
        ((decimal)day.UptimePercentage).Should().Be(50m);
    }

    [Fact]
    public void Calculate_GivenNoProbesAtAll_IsFullyAvailable()
    {
        // Act
        var day = Calculate([], [], EndOfDay);

        // Assert
        ((int)day.TotalProbes).Should().Be(0);
        ((int)day.DowntimeSeconds).Should().Be(0);
        ((decimal)day.UptimePercentage).Should().Be(100m);
    }

    private static ComponentUptimeDay Calculate(
        List<ComponentProbe> probes,
        List<MaintenanceWindow> maintenance,
        DateTimeOffset now)
    {
        return UptimeDayCalculator.Calculate(
            new ComponentUptimeDayId(Guid.NewGuid()),
            ServiceComponent.Api,
            new ObservationDate(Day),
            probes,
            maintenance,
            now);
    }

    private static List<ComponentProbe> Probes(params (int Minute, bool Healthy)[] probes)
    {
        return probes.Select(x => Probe(x.Minute, x.Healthy)).ToList();
    }

    private static ComponentProbe Probe(int minute, bool healthy)
    {
        var observedAt = DayStart.AddMinutes(minute);

        return new ComponentProbe
        {
            ComponentProbeId = new ComponentProbeId(Guid.NewGuid()),
            Component = ServiceComponent.Api,
            Source = ProbeSource.Internal,
            ObservedAt = new ObservedAt(observedAt),
            IsHealthy = new IsHealthy(healthy),
            LatencyMilliseconds = new LatencyMilliseconds(5),
            Detail = null,
            CreatedAt = new CreatedAt(observedAt),
            UpdatedAt = new UpdatedAt(observedAt)
        };
    }

    private static MaintenanceWindow Maintenance(DateTimeOffset start, DateTimeOffset end, ServiceComponent component)
    {
        return new MaintenanceWindow
        {
            MaintenanceWindowId = new MaintenanceWindowId(Guid.NewGuid()),
            Title = new MaintenanceWindowTitle("Planned restart"),
            Description = new MaintenanceWindowDescription("Rolling restart."),
            Status = MaintenanceStatus.Completed,
            Components = [component],
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
