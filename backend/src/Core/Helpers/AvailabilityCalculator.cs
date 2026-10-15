using Core.Models.Availability;
using Core.Models.Status;
using Core.ValueObjects;

namespace Core.Helpers;

public static class AvailabilityCalculator
{
    public static AvailabilityMeasurement Measure(
        OrganizationId organizationId,
        PeriodStart periodStart,
        PeriodEnd periodEnd,
        IReadOnlyList<AvailabilityWindow> windows,
        IReadOnlyList<MaintenanceWindow> announcedMaintenance,
        AvailabilityCommitment? commitment)
    {
        var counted = windows.Where(x => !IsFullyCovered(x, announcedMaintenance)).ToList();

        var totalRequests = counted.Sum(x => (long)(int)x.TotalRequests);
        var failedRequests = counted.Sum(x => (long)(int)x.FailedRequests);
        var totalLatency = counted.Sum(x => x.TotalLatencyMilliseconds);
        var maxLatency = counted.Count == 0 ? 0 : counted.Max(x => (int)x.MaxLatencyMilliseconds);

        var uptime = CalculateUptimePercentage(totalRequests, failedRequests);
        var meanLatency = totalRequests == 0 ? 0 : (int)(totalLatency / totalRequests);
        var percentile = commitment?.LatencyPercentile ?? new LatencyPercentile(99);
        var percentileLatency = CalculatePercentileLatency(counted, percentile, maxLatency);

        return new AvailabilityMeasurement
        {
            OrganizationId = organizationId,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            TotalRequests = new RequestCount((int)Math.Min(totalRequests, int.MaxValue)),
            FailedRequests = new RequestCount((int)Math.Min(failedRequests, int.MaxValue)),
            UptimePercentage = uptime,
            MeanLatencyMilliseconds = new LatencyMilliseconds(meanLatency),
            PercentileLatencyMilliseconds = new LatencyMilliseconds(percentileLatency),
            Percentile = percentile,
            MaxLatencyMilliseconds = new LatencyMilliseconds(maxLatency),
            AvailabilityTarget = commitment?.AvailabilityTarget,
            LatencyTarget = commitment?.LatencyTarget,
            MetAvailabilityTarget = commitment is null ? null : new MetTarget(uptime >= commitment.AvailabilityTarget),
            MetLatencyTarget = commitment is null ? null : new MetTarget(percentileLatency <= commitment.LatencyTarget)
        };
    }

    public static UptimePercentage CalculateUptimePercentage(long totalRequests, long failedRequests)
    {
        if (totalRequests <= 0)
        {
            return new UptimePercentage(100m);
        }

        var successful = Math.Max(totalRequests - failedRequests, 0);
        var percentage = successful / (decimal)totalRequests * 100m;

        return new UptimePercentage(Math.Round(percentage, 4, MidpointRounding.ToZero));
    }

    public static int CalculatePercentileLatency(
        IReadOnlyList<AvailabilityWindow> windows,
        LatencyPercentile percentile,
        int maxLatencyMilliseconds)
    {
        var totals = new SortedDictionary<int, long>();

        foreach (var bucket in windows.SelectMany(x => x.LatencyBuckets))
        {
            var bound = (int)bucket.UpperBoundMilliseconds;
            totals[bound] = totals.GetValueOrDefault(bound) + bucket.Count;
        }

        var totalCount = totals.Values.Sum();

        if (totalCount == 0)
        {
            return 0;
        }

        var rank = (long)Math.Ceiling((int)percentile / 100m * totalCount);
        var cumulative = 0L;

        foreach (var (bound, count) in totals)
        {
            cumulative += count;

            if (cumulative < rank)
            {
                continue;
            }

            // Rows from the old coarse histogram used int.MaxValue as an open-ended bucket.
            return bound == int.MaxValue ? maxLatencyMilliseconds : bound;
        }

        return maxLatencyMilliseconds;
    }

    private static bool IsFullyCovered(AvailabilityWindow window, IReadOnlyList<MaintenanceWindow> announcedMaintenance)
    {
        var start = (DateTimeOffset)window.PeriodStart;
        var end = (DateTimeOffset)window.PeriodEnd;

        return announcedMaintenance.Any(x =>
            x.ScheduledStart <= start &&
            x.ScheduledEnd >= end);
    }
}
