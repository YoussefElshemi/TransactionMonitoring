using Core.ValueObjects;

namespace Core.Models.Plans;

// Half-open: [FromQuantity, ToQuantity).
public record PlanPriceTier
{
    public required PlanPriceTierId PlanPriceTierId { get; init; }
    public required PlanPriceId PlanPriceId { get; init; }
    public required FromQuantity FromQuantity { get; init; }

    public required ToQuantity? ToQuantity { get; init; }

    public required UnitAmount UnitAmount { get; init; }

    public required FlatAmount FlatAmount { get; init; }

    public required CreatedAt CreatedAt { get; init; }
}
