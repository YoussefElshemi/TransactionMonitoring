using Core.ValueObjects;

namespace Core.Models.VelocityProfiles;

public record SetVelocityProfileRequest
{
    public required OrganizationId OrganizationId { get; init; }
    public required List<VelocityMetric> Metrics { get; init; }
    public required List<VelocityThreshold> Thresholds { get; init; }
    public required ApplicationUserId CreatedByUserId { get; init; }
}
