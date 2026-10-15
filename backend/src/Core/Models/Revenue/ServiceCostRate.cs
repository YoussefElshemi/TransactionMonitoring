using Core.Enums;
using Core.Models.BaseModels;
using Core.ValueObjects;

namespace Core.Models.Revenue;

public record ServiceCostRate : BaseModel
{
    public required ServiceCostRateId ServiceCostRateId { get; init; }
    public required BillableResource BillableResource { get; init; }
    public required UnitCost UnitCost { get; init; }

    public required Currency Currency { get; init; }

    public required EffectiveFrom EffectiveFrom { get; init; }
    public required ApplicationUserId? CreatedByUserId { get; init; }
}
