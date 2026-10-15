using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record AvailabilityWindowEntity : BaseEntity
{
    public required Guid AvailabilityWindowId { get; init; }
    public required Guid OrganizationId { get; init; }
    public OrganizationEntity Organization { get; init; } = null!;
    public required DateTimeOffset PeriodStart { get; init; }
    public required DateTimeOffset PeriodEnd { get; init; }
    public required int TotalRequests { get; set; }
    public required int FailedRequests { get; set; }
    public required long TotalLatencyMilliseconds { get; set; }
    public required int MaxLatencyMilliseconds { get; set; }
    public ICollection<AvailabilityWindowLatencyBucketEntity> LatencyBuckets { get; init; } = [];
}
