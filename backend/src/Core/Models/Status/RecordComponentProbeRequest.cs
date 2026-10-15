using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.Status;

public record RecordComponentProbeRequest
{
    public required ServiceComponent Component { get; init; }
    public required ProbeSource Source { get; init; }

    public required ObservedAt? ObservedAt { get; init; }
    public required IsHealthy IsHealthy { get; init; }
    public required LatencyMilliseconds LatencyMilliseconds { get; init; }
    public required ProbeDetail? Detail { get; init; }
}
