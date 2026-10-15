using Core.Enums;
using Core.Models.Rules;
using Core.Models.VelocityObservations;
using Core.ValueObjects;

namespace Core.Helpers;

public static class VelocityWindowResolver
{
    public static int MaxLength(VelocityWindowUnit unit)
    {
        return unit switch
        {
            VelocityWindowUnit.Minute => 527040,
            VelocityWindowUnit.Hour => 8784,
            VelocityWindowUnit.Day => 366,
            VelocityWindowUnit.Week => 53,
            _ => 12
        };
    }

    public static (DateTimeOffset From, DateTimeOffset To) Resolve(VelocityWindowModel window, DateTimeOffset now)
    {
        return window.Kind == VelocityWindowKind.Calendar
            ? (StartOfPeriod(window, now), now)
            : (Rewind(window.Unit, window.Length ?? 1, now), now);
    }

    public static VelocityWindowQuery Query(
        VelocityConditionModel velocity,
        IReadOnlyDictionary<string, decimal> thresholds,
        DateTimeOffset now,
        TransactionId? currentTransactionId)
    {
        var (from, to) = Resolve(velocity.Window ?? new VelocityWindowModel(), now);
        var (floor, ceiling) = Band(velocity, thresholds);

        return new VelocityWindowQuery
        {
            Role = velocity.Role,
            Metric = new VelocityMetricName(string.IsNullOrWhiteSpace(velocity.Metric) ? " " : velocity.Metric),
            From = new ObservedAt(from),
            To = new ObservedAt(to),
            IncludeCurrent = new IncludeCurrent(velocity.IncludeCurrent),
            ExcludeTransactionId = velocity.IncludeCurrent ? null : currentTransactionId,
            BandFloor = floor.HasValue ? new MetricNumber(floor.Value) : null,
            BandCeiling = ceiling.HasValue ? new MetricNumber(ceiling.Value) : null
        };
    }

    private static (decimal? Floor, decimal? Ceiling) Band(VelocityConditionModel velocity, IReadOnlyDictionary<string, decimal> thresholds)
    {
        if (velocity.Function != VelocityFunction.StructuringCount ||
            string.IsNullOrWhiteSpace(velocity.Threshold) ||
            !velocity.Band.HasValue ||
            !thresholds.TryGetValue(velocity.Threshold, out var threshold))
        {
            return (null, null);
        }

        var (floor, ceiling) = VelocityAggregator.Band(threshold, velocity.Band.Value);

        return (floor, ceiling);
    }

    public static DateTimeOffset Widest(IEnumerable<VelocityWindowModel> windows, DateTimeOffset now)
    {
        var from = now;

        foreach (var window in windows)
        {
            var (windowFrom, _) = Resolve(window, now);

            if (windowFrom < from)
            {
                from = windowFrom;
            }
        }

        return from;
    }

    private static DateTimeOffset Rewind(VelocityWindowUnit unit, int length, DateTimeOffset now)
    {
        return unit switch
        {
            VelocityWindowUnit.Minute => now.AddMinutes(-length),
            VelocityWindowUnit.Hour => now.AddHours(-length),
            VelocityWindowUnit.Day => now.AddDays(-length),
            VelocityWindowUnit.Week => now.AddDays(-7 * length),
            _ => now.AddMonths(-length)
        };
    }

    private static DateTimeOffset StartOfPeriod(VelocityWindowModel window, DateTimeOffset now)
    {
        var timeZone = ResolveTimeZone(window.TimeZone);
        var local = TimeZoneInfo.ConvertTime(now, timeZone);

        var start = window.Unit switch
        {
            VelocityWindowUnit.Minute => new DateTime(local.Year, local.Month, local.Day, local.Hour, local.Minute, 0),
            VelocityWindowUnit.Hour => new DateTime(local.Year, local.Month, local.Day, local.Hour, 0, 0),
            VelocityWindowUnit.Day => new DateTime(local.Year, local.Month, local.Day, 0, 0, 0),
            VelocityWindowUnit.Week => new DateTime(local.Year, local.Month, local.Day, 0, 0, 0)
                .AddDays(-(((int)local.DayOfWeek + 6) % 7)),
            _ => new DateTime(local.Year, local.Month, 1, 0, 0, 0)
        };

        return new DateTimeOffset(start, timeZone.GetUtcOffset(start));
    }

    // An unknown zone falls back to UTC rather than failing the payment.
    private static TimeZoneInfo ResolveTimeZone(string? timeZone)
    {
        if (string.IsNullOrWhiteSpace(timeZone))
        {
            return TimeZoneInfo.Utc;
        }

        return TimeZoneInfo.TryFindSystemTimeZoneById(timeZone, out var resolved) ? resolved : TimeZoneInfo.Utc;
    }
}
