using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record ComponentProbeEntity : BaseEntity
{
    public required Guid ComponentProbeId { get; init; }
    public required int ComponentId { get; init; }
    public ServiceComponentEntity Component { get; init; } = null!;
    public required int SourceId { get; init; }
    public ProbeSourceEntity Source { get; init; } = null!;
    public required DateTimeOffset ObservedAt { get; init; }
    public required bool IsHealthy { get; init; }
    public required int LatencyMilliseconds { get; init; }
    public required string? Detail { get; init; }
}
