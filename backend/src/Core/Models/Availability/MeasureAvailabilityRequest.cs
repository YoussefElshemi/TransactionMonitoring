using Core.ValueObjects;

namespace Core.Models.Availability;

public record MeasureAvailabilityRequest
{
    public required OrganizationId OrganizationId { get; init; }
    public required PeriodStart PeriodStart { get; init; }
    public required PeriodEnd PeriodEnd { get; init; }
}
