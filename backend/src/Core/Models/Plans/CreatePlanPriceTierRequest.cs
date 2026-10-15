using Core.ValueObjects;

namespace Core.Models.Plans;

public record CreatePlanPriceTierRequest
{
    public required FromQuantity FromQuantity { get; init; }
    public required ToQuantity? ToQuantity { get; init; }
    public required UnitAmount UnitAmount { get; init; }
    public required FlatAmount FlatAmount { get; init; }
}
