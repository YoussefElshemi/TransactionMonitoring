using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.Revenue;

public record CreateServiceCostRateRequest
{
    public required BillableResource BillableResource { get; init; }
    public required UnitCost UnitCost { get; init; }
    public required Currency Currency { get; init; }

    public required EffectiveFrom? EffectiveFrom { get; init; }

    public required ApplicationUserId? CreatedByUserId { get; init; }
}
