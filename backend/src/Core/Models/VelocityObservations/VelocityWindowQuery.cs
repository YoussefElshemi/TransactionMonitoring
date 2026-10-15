using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.VelocityObservations;

public record VelocityWindowQuery
{
    public required PartyRole Role { get; init; }
    public required VelocityMetricName Metric { get; init; }
    public required ObservedAt From { get; init; }
    public required ObservedAt To { get; init; }
    public required IncludeCurrent IncludeCurrent { get; init; }

    public required TransactionId? ExcludeTransactionId { get; init; }

    public required MetricNumber? BandFloor { get; init; }
    public required MetricNumber? BandCeiling { get; init; }
}
