using Core.ValueObjects;

namespace Core.Models.Availability;

public record AvailabilityMeasurement
{
    public required OrganizationId OrganizationId { get; init; }
    public required PeriodStart PeriodStart { get; init; }
    public required PeriodEnd PeriodEnd { get; init; }

    public required RequestCount TotalRequests { get; init; }
    public required RequestCount FailedRequests { get; init; }

    public required UptimePercentage UptimePercentage { get; init; }

    public required LatencyMilliseconds MeanLatencyMilliseconds { get; init; }

    public required LatencyMilliseconds PercentileLatencyMilliseconds { get; init; }

    public required LatencyPercentile Percentile { get; init; }
    public required LatencyMilliseconds MaxLatencyMilliseconds { get; init; }

    public required AvailabilityTarget? AvailabilityTarget { get; init; }
    public required LatencyTarget? LatencyTarget { get; init; }
    public required MetTarget? MetAvailabilityTarget { get; init; }
    public required MetTarget? MetLatencyTarget { get; init; }
}
