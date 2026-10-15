using Core.Enums;
using Core.Models.Status;
using Core.ValueObjects;

namespace Core.Helpers;

public static class UptimeDayCalculator
{
    public static ComponentUptimeDay Calculate(
        ComponentUptimeDayId componentUptimeDayId,
        ServiceComponent component,
        ObservationDate date,
        IReadOnlyList<ComponentProbe> probes,
        IReadOnlyList<MaintenanceWindow> announcedMaintenance,
        DateTimeOffset now)
    {
        var dayStart = new DateTimeOffset(((DateOnly)date).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var dayEnd = dayStart.AddDays(1);

        var counted = probes
            .Where(x => !IsUnderMaintenance(x, component, announcedMaintenance))
            .OrderBy(x => (DateTimeOffset)x.ObservedAt)
            .ToList();

        var healthy = counted.Count(x => x.IsHealthy);
        var downtimeSeconds = 0d;

        for (var index = 0; index < counted.Count; index++)
        {
            if (counted[index].IsHealthy)
            {
                continue;
            }

            var from = (DateTimeOffset)counted[index].ObservedAt;
            var to = index + 1 < counted.Count
                ? counted[index + 1].ObservedAt
                : Min(dayEnd, Max(now, from));

            downtimeSeconds += (to - from).TotalSeconds;
        }

        return new ComponentUptimeDay
        {
            ComponentUptimeDayId = componentUptimeDayId,
            Component = component,
            Date = date,
            TotalProbes = new ProbeCount(counted.Count),
            HealthyProbes = new ProbeCount(healthy),
            DowntimeSeconds = new DowntimeSeconds((int)Math.Round(downtimeSeconds, MidpointRounding.AwayFromZero)),

            UptimePercentage = AvailabilityCalculator.CalculateUptimePercentage(counted.Count, counted.Count - healthy),
            CreatedAt = new CreatedAt(now),
            UpdatedAt = new UpdatedAt(now)
        };
    }

    private static bool IsUnderMaintenance(
        ComponentProbe probe,
        ServiceComponent component,
        IReadOnlyList<MaintenanceWindow> announcedMaintenance)
    {
        var observedAt = (DateTimeOffset)probe.ObservedAt;

        return announcedMaintenance.Any(x =>
            x.Components.Contains(component) &&
            x.ScheduledStart <= observedAt &&
            x.ScheduledEnd > observedAt);
    }

    private static DateTimeOffset Min(DateTimeOffset first, DateTimeOffset second)
    {
        return first < second ? first : second;
    }

    private static DateTimeOffset Max(DateTimeOffset first, DateTimeOffset second)
    {
        return first > second ? first : second;
    }
}
