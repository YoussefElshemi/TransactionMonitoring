using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.Revenue;

public record OrganizationResourceUsage
{
    public required OrganizationId OrganizationId { get; init; }
    public required BillableResource BillableResource { get; init; }
    public required Count Count { get; init; }
}
