using Core.Models.Plans;
using Core.ValueObjects;
using Presentation.Models.Plans;

namespace Presentation.Mappers.Plans;

public static class PlanPriceMapper
{
    public static List<CreatePlanPriceRequest> ToModel(this List<PlanPriceRequestDto> dtos)
    {
        return dtos.ConvertAll(x => new CreatePlanPriceRequest
        {
            BillableResource = x.BillableResource,
            PricingModel = x.PricingModel,
            FreeAllowance = new FreeAllowance(x.FreeAllowance),
            Tiers = x.Tiers.ConvertAll(tier => new CreatePlanPriceTierRequest
            {
                FromQuantity = new FromQuantity(tier.FromQuantity),
                ToQuantity = tier.ToQuantity.HasValue ? new ToQuantity(tier.ToQuantity.Value) : null,
                UnitAmount = new UnitAmount(tier.UnitAmount),
                FlatAmount = new FlatAmount(tier.FlatAmount)
            })
        });
    }

    public static PlanPriceResponseDto ToDto(this PlanPrice planPrice)
    {
        return new PlanPriceResponseDto
        {
            PlanPriceId = planPrice.PlanPriceId,
            PlanVersionId = planPrice.PlanVersionId,
            BillableResource = planPrice.BillableResource?.ToString(),
            PricingModel = planPrice.PricingModel.ToString(),
            FreeAllowance = planPrice.FreeAllowance,
            Tiers = planPrice.Tiers.ConvertAll(x => new PlanPriceTierResponseDto
            {
                PlanPriceTierId = x.PlanPriceTierId,
                FromQuantity = x.FromQuantity,
                ToQuantity = x.ToQuantity.HasValue ? x.ToQuantity.Value : null,
                UnitAmount = x.UnitAmount,
                FlatAmount = x.FlatAmount
            }),
            CreatedAt = planPrice.CreatedAt
        };
    }
}
