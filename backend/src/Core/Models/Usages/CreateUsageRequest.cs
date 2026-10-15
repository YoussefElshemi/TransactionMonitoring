using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.Usages;

public record CreateUsageRequest
{
    public required Resource Resource { get; init; }
    public required BillableResource BillableResource { get; init; }
    public required OrganizationId OrganizationId { get; init; }
    public required Count Count { get; init; }
}