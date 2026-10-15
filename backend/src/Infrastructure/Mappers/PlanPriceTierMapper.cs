using Core.Models.Plans;
using Core.ValueObjects;
using Infrastructure.Entities;

namespace Infrastructure.Mappers;

public static class PlanPriceTierMapper
{
    public static PlanPriceTier ToModel(this PlanPriceTierEntity planPriceTierEntity)
    {
        return new PlanPriceTier
        {
            PlanPriceTierId = new PlanPriceTierId(planPriceTierEntity.PlanPriceTierId),
            PlanPriceId = new PlanPriceId(planPriceTierEntity.PlanPriceId),
            FromQuantity = new FromQuantity(planPriceTierEntity.FromQuantity),
            ToQuantity = planPriceTierEntity.ToQuantity.HasValue ? new ToQuantity(planPriceTierEntity.ToQuantity.Value) : null,
            UnitAmount = new UnitAmount(planPriceTierEntity.UnitAmount),
            FlatAmount = new FlatAmount(planPriceTierEntity.FlatAmount),
            CreatedAt = new CreatedAt(planPriceTierEntity.CreatedAt)
        };
    }

    public static PlanPriceTierEntity ToEntity(this PlanPriceTier planPriceTier)
    {
        return new PlanPriceTierEntity
        {
            PlanPriceTierId = planPriceTier.PlanPriceTierId,
            PlanPriceId = planPriceTier.PlanPriceId,
            FromQuantity = planPriceTier.FromQuantity,
            ToQuantity = planPriceTier.ToQuantity.HasValue ? planPriceTier.ToQuantity.Value : null,
            UnitAmount = planPriceTier.UnitAmount,
            FlatAmount = planPriceTier.FlatAmount,
            CreatedAt = planPriceTier.CreatedAt
        };
    }
}
