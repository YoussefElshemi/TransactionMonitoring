using Core.ValueObjects;

namespace Core.Models.Availability;

public record MergeAvailabilityLatencyBucketRequest
{
    public required LatencyBucketBound UpperBoundMilliseconds { get; init; }
    public required RequestCount Count { get; init; }
}
