using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.Status;

public record ComponentHealth
{
    public required ServiceComponent Component { get; init; }
    public required ComponentStatus Status { get; init; }
    public required ObservedAt? LastProbedAt { get; init; }
    public required LatencyMilliseconds? LatencyMilliseconds { get; init; }
    public required UptimePercentage UptimePercentage { get; init; }
    public required List<ComponentUptimeDay> History { get; init; }
}
