using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.Revenue;

public record HealthSignal
{
    public required HealthSignalKind Kind { get; init; }
    public required ScorePenalty Penalty { get; init; }
    public required SignalDetail Detail { get; init; }
}
