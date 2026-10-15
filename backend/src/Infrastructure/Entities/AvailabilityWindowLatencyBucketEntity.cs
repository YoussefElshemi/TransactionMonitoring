using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record AvailabilityWindowLatencyBucketEntity : BaseEntity
{
    public required Guid AvailabilityWindowId { get; init; }
    public AvailabilityWindowEntity AvailabilityWindow { get; init; } = null!;
    public required int UpperBoundMilliseconds { get; init; }
    public required int Count { get; set; }
}
