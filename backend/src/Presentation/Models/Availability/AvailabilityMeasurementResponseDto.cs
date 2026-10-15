namespace Presentation.Models.Availability;

public record AvailabilityMeasurementResponseDto
{
    public required Guid OrganizationId { get; init; }
    public required DateTimeOffset PeriodStart { get; init; }
    public required DateTimeOffset PeriodEnd { get; init; }
    public required int TotalRequests { get; init; }
    public required int FailedRequests { get; init; }
    public required decimal UptimePercentage { get; init; }
    public required int MeanLatencyMilliseconds { get; init; }

    public required int PercentileLatencyMilliseconds { get; init; }

    public required int Percentile { get; init; }
    public required int MaxLatencyMilliseconds { get; init; }

    public required decimal? AvailabilityTarget { get; init; }
    public required int? LatencyTargetMilliseconds { get; init; }
    public required bool? MetAvailabilityTarget { get; init; }
    public required bool? MetLatencyTarget { get; init; }
}
