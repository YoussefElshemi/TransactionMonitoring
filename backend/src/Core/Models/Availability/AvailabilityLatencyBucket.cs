using Core.Models.BaseModels;
using Core.ValueObjects;

namespace Core.Models.Availability;

public record AvailabilityLatencyBucket : BaseModel
{
    public required AvailabilityWindowId AvailabilityWindowId { get; init; }
    public required LatencyBucketBound UpperBoundMilliseconds { get; init; }
    public required RequestCount Count { get; init; }
}
