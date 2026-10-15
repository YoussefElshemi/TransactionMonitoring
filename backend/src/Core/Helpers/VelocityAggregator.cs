using Core.Enums;
using Core.Models.VelocityObservations;
using Core.ValueObjects;

namespace Core.Helpers;

public static class VelocityAggregator
{
    public static object? Aggregate(VelocityFunction function, VelocityWindowQuery query, VelocityAggregate aggregate)
    {
        return function switch
        {
            VelocityFunction.Count => (decimal)(int)aggregate.Count,
            VelocityFunction.DistinctCount => (decimal)(int)aggregate.DistinctCount,
            VelocityFunction.StructuringCount => query.BandFloor.HasValue ? (decimal)(int)aggregate.BandCount : null,
            VelocityFunction.Sum => Number(aggregate.Sum),
            VelocityFunction.Min => Number(aggregate.Min),
            VelocityFunction.Max => Number(aggregate.Max),
            VelocityFunction.Average => Average(aggregate),
            _ => null
        };
    }

    public static VelocityAggregate Reduce(IReadOnlyList<VelocityObservationFact> observations, VelocityWindowQuery query)
    {
        var selected = observations.Where(x => Selects(query, x)).ToList();
        var numbers = selected.Where(x => x.Number.HasValue).Select(x => (decimal)x.Number!.Value).ToList();

        return new VelocityAggregate
        {
            Count = new Count(selected.Count),
            NumberCount = new Count(numbers.Count),
            DistinctCount = new Count(selected.Select(x => (string)x.Text).Distinct(StringComparer.Ordinal).Count()),
            BandCount = new Count(query.BandFloor.HasValue && query.BandCeiling.HasValue
                ? numbers.Count(x => x >= query.BandFloor.Value && x < query.BandCeiling.Value)
                : 0),
            Sum = numbers.Count == 0 ? null : new MetricNumber(numbers.Sum()),
            Min = numbers.Count == 0 ? null : new MetricNumber(numbers.Min()),
            Max = numbers.Count == 0 ? null : new MetricNumber(numbers.Max())
        };
    }

    public static (decimal Floor, decimal Ceiling) Band(decimal threshold, decimal band)
    {
        return (threshold - threshold * band, threshold);
    }

    private static bool Selects(VelocityWindowQuery query, VelocityObservationFact observation)
    {
        return observation.Role == query.Role &&
               string.Equals(observation.Metric, query.Metric, StringComparison.OrdinalIgnoreCase) &&
               (DateTimeOffset)observation.ObservedAt >= query.From &&
               (DateTimeOffset)observation.ObservedAt <= query.To &&
               (query.IncludeCurrent || !observation.IsCurrent);
    }

    private static object? Average(VelocityAggregate aggregate)
    {
        if (!aggregate.Sum.HasValue || aggregate.NumberCount == 0)
        {
            return null;
        }

        return aggregate.Sum.Value / (int)aggregate.NumberCount;
    }

    private static object? Number(MetricNumber? value)
    {
        return value.HasValue ? (decimal)value.Value : null;
    }
}
