using Core.ValueObjects;

namespace Core.Models.VelocityObservations;

public record VelocityAggregate
{
    public required Count Count { get; init; }
    public required Count NumberCount { get; init; }
    public required Count DistinctCount { get; init; }
    public required Count BandCount { get; init; }
    public required MetricNumber? Sum { get; init; }
    public required MetricNumber? Min { get; init; }
    public required MetricNumber? Max { get; init; }

    public static VelocityAggregate Empty { get; } = new()
    {
        Count = new Count(0),
        NumberCount = new Count(0),
        DistinctCount = new Count(0),
        BandCount = new Count(0),
        Sum = null,
        Min = null,
        Max = null
    };
}

public record VelocityAggregateRow
{
    public required PartyId PartyId { get; init; }
    public required VelocityAggregate Aggregate { get; init; }
}
