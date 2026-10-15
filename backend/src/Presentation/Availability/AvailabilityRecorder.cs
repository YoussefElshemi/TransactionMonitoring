using System.Collections.Concurrent;
using Core.Models.Availability;
using Core.ValueObjects;

namespace Presentation.Availability;

public sealed class AvailabilityRecorder
{
    private readonly ConcurrentDictionary<(Guid OrganizationId, DateTimeOffset PeriodStart), AvailabilityBucket> _buckets = new();

    public void Record(Guid organizationId, DateTimeOffset observedAt, long elapsedMilliseconds, bool isFailure)
    {
        var periodStart = ToPeriodStart(observedAt);
        var bucket = _buckets.GetOrAdd((organizationId, periodStart), _ => new AvailabilityBucket());

        bucket.Record(elapsedMilliseconds, isFailure);
    }

    public IReadOnlyList<MergeAvailabilityWindowRequest> Drain()
    {
        var drained = new List<MergeAvailabilityWindowRequest>();

        foreach (var key in _buckets.Keys.ToList())
        {
            if (!_buckets.TryRemove(key, out var bucket))
            {
                continue;
            }

            var snapshot = bucket.Snapshot();

            if (snapshot.TotalRequests == 0)
            {
                continue;
            }

            drained.Add(new MergeAvailabilityWindowRequest
            {
                OrganizationId = new OrganizationId(key.OrganizationId),
                PeriodStart = new PeriodStart(key.PeriodStart),
                TotalRequests = new RequestCount(snapshot.TotalRequests),
                FailedRequests = new RequestCount(snapshot.FailedRequests),
                TotalLatencyMilliseconds = new TotalLatencyMilliseconds(snapshot.TotalLatencyMilliseconds),
                MaxLatencyMilliseconds = new LatencyMilliseconds(snapshot.MaxLatencyMilliseconds),
                LatencyBuckets = snapshot.LatencyCounts
                    .OrderBy(x => x.Key)
                    .Select(x => new MergeAvailabilityLatencyBucketRequest
                    {
                        UpperBoundMilliseconds = new LatencyBucketBound(x.Key),
                        Count = new RequestCount(x.Value)
                    })
                    .ToList()
            });
        }

        return drained;
    }

    // Every instance must agree where an hour begins.
    public static DateTimeOffset ToPeriodStart(DateTimeOffset observedAt)
    {
        var utc = observedAt.ToUniversalTime();

        return new DateTimeOffset(utc.Year, utc.Month, utc.Day, utc.Hour, 0, 0, TimeSpan.Zero);
    }
}
