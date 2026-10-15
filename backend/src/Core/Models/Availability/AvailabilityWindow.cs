using Core.Models.BaseModels;
using Core.ValueObjects;

namespace Core.Models.Availability;

public record AvailabilityWindow : BaseModel
{
    public required AvailabilityWindowId AvailabilityWindowId { get; init; }
    public required OrganizationId OrganizationId { get; init; }
    public required PeriodStart PeriodStart { get; init; }
    public required PeriodEnd PeriodEnd { get; init; }

    public required RequestCount TotalRequests { get; init; }

    // Server failures only: 4xx and 429 are the caller's, and counting them would let a client manufacture a credit.
    public required RequestCount FailedRequests { get; init; }

    public required TotalLatencyMilliseconds TotalLatencyMilliseconds { get; init; }
    public required LatencyMilliseconds MaxLatencyMilliseconds { get; init; }

    public required List<AvailabilityLatencyBucket> LatencyBuckets { get; init; }
}
