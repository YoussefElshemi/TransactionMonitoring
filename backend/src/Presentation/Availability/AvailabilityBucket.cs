using Core.Constants;

namespace Presentation.Availability;

// Locked, not interlocked: five fields must move together.
internal sealed class AvailabilityBucket
{
    private readonly object _lock = new();
    private readonly Dictionary<int, int> _latencyCounts = [];

    private int _totalRequests;
    private int _failedRequests;
    private long _totalLatencyMilliseconds;
    private int _maxLatencyMilliseconds;

    public void Record(long elapsedMilliseconds, bool isFailure)
    {
        var clamped = (int)Math.Clamp(elapsedMilliseconds, 0, int.MaxValue);
        var bucketUpperBound = ResolveBucketUpperBound(clamped);

        lock (_lock)
        {
            _totalRequests++;

            if (isFailure)
            {
                _failedRequests++;
            }

            _totalLatencyMilliseconds += clamped;
            _maxLatencyMilliseconds = Math.Max(_maxLatencyMilliseconds, clamped);
            _latencyCounts[bucketUpperBound] = _latencyCounts.GetValueOrDefault(bucketUpperBound) + 1;
        }
    }

    public AvailabilitySnapshot Snapshot()
    {
        lock (_lock)
        {
            return new AvailabilitySnapshot(
                _totalRequests,
                _failedRequests,
                _totalLatencyMilliseconds,
                _maxLatencyMilliseconds,
                new Dictionary<int, int>(_latencyCounts));
        }
    }

    private static int ResolveBucketUpperBound(int latencyMilliseconds)
    {
        var width = AvailabilityConstants.LatencyBucketWidthMilliseconds;
        var bucketStart = (long)latencyMilliseconds / width * width;

        return (int)Math.Min(bucketStart + width - 1, int.MaxValue);
    }
}

internal readonly record struct AvailabilitySnapshot(
    int TotalRequests,
    int FailedRequests,
    long TotalLatencyMilliseconds,
    int MaxLatencyMilliseconds,
    IReadOnlyDictionary<int, int> LatencyCounts);
