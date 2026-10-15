using Core.Enums;
using Core.Models.BaseModels;
using Core.ValueObjects;

namespace Core.Models.VelocityObservations;

public record VelocityObservation : BaseModel
{
    public required VelocityObservationId VelocityObservationId { get; init; }
    public required OrganizationId OrganizationId { get; init; }
    public required PartyId PartyId { get; init; }
    public required PartyRole Role { get; init; }
    public required VelocityMetricName Metric { get; init; }
    public required TransactionId TransactionId { get; init; }

    // The transaction's own clock, not write time, so re-evaluations and backfills land in the right window.
    public required ObservedAt ObservedAt { get; init; }

    public required MetricText Text { get; init; }
    public required MetricNumber? Number { get; init; }

    public required Mode Mode { get; init; }
}
