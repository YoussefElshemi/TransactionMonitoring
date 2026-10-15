using Core.Enums;
using Core.Models.Plans;
using Core.ValueObjects;

namespace Core.Factories;

public static class PlanPriceFactory
{
    public static List<PlanPrice> Create(
        PlanVersionId planVersionId,
        CostPerRequest costPerRequest,
        List<CreatePlanPriceRequest> requests,
        TimeProvider timeProvider)
    {
        var now = timeProvider.GetUtcNow();

        if (requests.Count == 0)
        {
            requests =
            [
                new CreatePlanPriceRequest
                {
                    BillableResource = null,
                    PricingModel = PricingModel.Flat,
                    FreeAllowance = new FreeAllowance(0),
                    Tiers =
                    [
                        new CreatePlanPriceTierRequest
                        {
                            FromQuantity = new FromQuantity(0),
                            ToQuantity = null,
                            UnitAmount = new UnitAmount(costPerRequest),
                            FlatAmount = new FlatAmount(0)
                        }
                    ]
                }
            ];
        }

        return requests.ConvertAll(x => Create(planVersionId, x, now));
    }

    public static CostPerRequest HeadlineRate(CostPerRequest costPerRequest, List<CreatePlanPriceRequest> requests)
    {
        var catchAll = requests.Find(x => x.BillableResource is null);

        if (catchAll is null)
        {
            return costPerRequest;
        }

        var bottomBand = catchAll.Tiers.OrderBy(x => (int)x.FromQuantity).FirstOrDefault();

        return bottomBand is null ? costPerRequest : new CostPerRequest(bottomBand.UnitAmount);
    }

    private static PlanPrice Create(PlanVersionId planVersionId, CreatePlanPriceRequest request, DateTimeOffset now)
    {
        var planPriceId = new PlanPriceId(Guid.NewGuid());

        return new PlanPrice
        {
            PlanPriceId = planPriceId,
            PlanVersionId = planVersionId,
            BillableResource = request.BillableResource,
            PricingModel = request.PricingModel,
            FreeAllowance = request.FreeAllowance,
            Tiers = request.Tiers.ConvertAll(x => new PlanPriceTier
            {
                PlanPriceTierId = new PlanPriceTierId(Guid.NewGuid()),
                PlanPriceId = planPriceId,
                FromQuantity = x.FromQuantity,
                ToQuantity = x.ToQuantity,
                UnitAmount = x.UnitAmount,
                FlatAmount = x.FlatAmount,
                CreatedAt = new CreatedAt(now)
            }),
            CreatedAt = new CreatedAt(now)
        };
    }
}
