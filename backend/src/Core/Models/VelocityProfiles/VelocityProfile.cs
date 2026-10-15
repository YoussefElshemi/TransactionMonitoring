using Core.Enums;
using Core.Models.BaseModels;
using Core.ValueObjects;

namespace Core.Models.VelocityProfiles;

public record VelocityProfile : DeletableModel
{
    public required VelocityProfileId VelocityProfileId { get; init; }
    public required OrganizationId OrganizationId { get; init; }
    public required List<VelocityMetric> Metrics { get; init; }
    public required List<VelocityThreshold> Thresholds { get; init; }
    public required ApplicationUserId CreatedByUserId { get; init; }

    public required Mode Mode { get; init; }
}
