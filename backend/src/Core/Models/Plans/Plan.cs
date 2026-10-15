using Core.Models.BaseModels;
using Core.ValueObjects;

namespace Core.Models.Plans;

public record Plan : DeletableModel
{
    public required PlanId PlanId { get; init; }
    public required PlanName Name { get; init; }
    public required PlanDescription? Description { get; init; }

    public required OrganizationId? OrganizationId { get; init; }

    public required IsEnabled IsEnabled { get; init; }

    public required VersionNumber CurrentVersionNumber { get; init; }
    public required PlanVersion? CurrentVersion { get; init; }
}
