using Core.Enums;
using Core.Models.BaseModels;
using Core.ValueObjects;

namespace Core.Models.Usages;

public record Usage : BaseModel
{
    public required UsageId UsageId { get; init; }
    public required Resource Resource { get; init; }

    public required BillableResource BillableResource { get; init; }
    public required OrganizationId OrganizationId { get; init; }
    public required Count Count { get; init; }

    public required Mode Mode { get; init; }
}