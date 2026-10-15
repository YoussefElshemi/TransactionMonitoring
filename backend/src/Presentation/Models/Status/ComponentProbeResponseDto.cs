namespace Presentation.Models.Status;

public record ComponentProbeResponseDto
{
    public required Guid ComponentProbeId { get; init; }
    public required string Component { get; init; }
    public required string Source { get; init; }
    public required DateTimeOffset ObservedAt { get; init; }
    public required bool IsHealthy { get; init; }
    public required int LatencyMilliseconds { get; init; }
    public required string? Detail { get; init; }
}
