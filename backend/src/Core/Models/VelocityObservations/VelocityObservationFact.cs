using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.VelocityObservations;

public record VelocityObservationFact
{
    public required PartyId PartyId { get; init; }
    public required PartyRole Role { get; init; }
    public required VelocityMetricName Metric { get; init; }
    public required ObservedAt ObservedAt { get; init; }
    public required MetricText Text { get; init; }
    public required MetricNumber? Number { get; init; }

    public required IsCurrent IsCurrent { get; init; }
}
