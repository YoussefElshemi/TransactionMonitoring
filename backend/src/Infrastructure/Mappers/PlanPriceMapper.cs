using Core.Enums;
using Core.Models.Plans;
using Core.ValueObjects;
using Infrastructure.Entities;

namespace Infrastructure.Mappers;

public static class PlanPriceMapper
{
    public static PlanPrice ToModel(this PlanPriceEntity planPriceEntity)
    {
        return new PlanPrice
        {
            PlanPriceId = new PlanPriceId(planPriceEntity.PlanPriceId),
            PlanVersionId = new PlanVersionId(planPriceEntity.PlanVersionId),
            BillableResource = planPriceEntity.BillableResourceId.HasValue ? (BillableResource)planPriceEntity.BillableResourceId.Value : null,
            PricingModel = (PricingModel)planPriceEntity.PricingModelId,
            FreeAllowance = new FreeAllowance(planPriceEntity.FreeAllowance),
            Tiers = planPriceEntity.Tiers
                .OrderBy(x => x.FromQuantity)
                .Select(x => x.ToModel())
                .ToList(),
            CreatedAt = new CreatedAt(planPriceEntity.CreatedAt)
        };
    }

    public static PlanPriceEntity ToEntity(this PlanPrice planPrice)
    {
        return new PlanPriceEntity
        {
            PlanPriceId = planPrice.PlanPriceId,
            PlanVersionId = planPrice.PlanVersionId,
            BillableResourceId = planPrice.BillableResource.HasValue ? (int)planPrice.BillableResource.Value : null,
            PricingModelId = (int)planPrice.PricingModel,
            FreeAllowance = planPrice.FreeAllowance,
            CreatedAt = planPrice.CreatedAt
        };
    }
}
