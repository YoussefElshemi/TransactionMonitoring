using Core.Models.Availability;
using Core.ValueObjects;
using Infrastructure.Entities;

namespace Infrastructure.Mappers;

public static class AvailabilityWindowMapper
{
    public static AvailabilityWindow ToModel(this AvailabilityWindowEntity availabilityWindowEntity)
    {
        return new AvailabilityWindow
        {
            AvailabilityWindowId = new AvailabilityWindowId(availabilityWindowEntity.AvailabilityWindowId),
            OrganizationId = new OrganizationId(availabilityWindowEntity.OrganizationId),
            PeriodStart = new PeriodStart(availabilityWindowEntity.PeriodStart),
            PeriodEnd = new PeriodEnd(availabilityWindowEntity.PeriodEnd),
            TotalRequests = new RequestCount(availabilityWindowEntity.TotalRequests),
            FailedRequests = new RequestCount(availabilityWindowEntity.FailedRequests),
            TotalLatencyMilliseconds = new TotalLatencyMilliseconds(availabilityWindowEntity.TotalLatencyMilliseconds),
            MaxLatencyMilliseconds = new LatencyMilliseconds(availabilityWindowEntity.MaxLatencyMilliseconds),
            LatencyBuckets = availabilityWindowEntity.LatencyBuckets
                .OrderBy(x => x.UpperBoundMilliseconds)
                .Select(x => x.ToModel())
                .ToList(),
            CreatedAt = new CreatedAt(availabilityWindowEntity.CreatedAt),
            UpdatedAt = new UpdatedAt(availabilityWindowEntity.UpdatedAt)
        };
    }

    public static AvailabilityLatencyBucket ToModel(this AvailabilityWindowLatencyBucketEntity bucketEntity)
    {
        return new AvailabilityLatencyBucket
        {
            AvailabilityWindowId = new AvailabilityWindowId(bucketEntity.AvailabilityWindowId),
            UpperBoundMilliseconds = new LatencyBucketBound(bucketEntity.UpperBoundMilliseconds),
            Count = new RequestCount(bucketEntity.Count),
            CreatedAt = new CreatedAt(bucketEntity.CreatedAt),
            UpdatedAt = new UpdatedAt(bucketEntity.UpdatedAt)
        };
    }
}
