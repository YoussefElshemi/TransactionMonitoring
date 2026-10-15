using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.Rules;

public record VelocityCondition
{
    public required PartyRole Role { get; init; }
    public required VelocityMetricName Metric { get; init; }
    public required VelocityFunction Function { get; init; }
    public required VelocityWindow Window { get; init; }
    public required IncludeCurrent IncludeCurrent { get; init; }

    public required VelocityThresholdName? Threshold { get; init; }
    public required StructuringBand? Band { get; init; }
}
