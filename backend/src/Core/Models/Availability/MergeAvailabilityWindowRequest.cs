using Core.ValueObjects;

namespace Core.Models.Availability;

public record MergeAvailabilityWindowRequest
{
    public required OrganizationId OrganizationId { get; init; }
    public required PeriodStart PeriodStart { get; init; }
    public required RequestCount TotalRequests { get; init; }
    public required RequestCount FailedRequests { get; init; }
    public required TotalLatencyMilliseconds TotalLatencyMilliseconds { get; init; }
    public required LatencyMilliseconds MaxLatencyMilliseconds { get; init; }

    public required List<MergeAvailabilityLatencyBucketRequest> LatencyBuckets { get; init; }
}
