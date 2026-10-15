using Core.Enums;
using Core.Models.Plans;
using Core.ValueObjects;
using Infrastructure.Entities;

namespace Infrastructure.Mappers;

public static class PlanVersionMapper
{
    public static PlanVersion ToModel(this PlanVersionEntity planVersionEntity)
    {
        return new PlanVersion
        {
            PlanVersionId = new PlanVersionId(planVersionEntity.PlanVersionId),
            PlanId = new PlanId(planVersionEntity.PlanId),
            VersionNumber = new VersionNumber(planVersionEntity.VersionNumber),
            Currency = (Currency)planVersionEntity.CurrencyId,
            CostPerRequest = new CostPerRequest(planVersionEntity.CostPerRequest),
            PlatformFee = new PlatformFee(planVersionEntity.PlatformFee),
            MinimumCharge = new MinimumCharge(planVersionEntity.MinimumCharge),
            Prices = planVersionEntity.Prices.Select(x => x.ToModel()).ToList(),
            PublishedByUserId = planVersionEntity.PublishedByUserId.HasValue ? new ApplicationUserId(planVersionEntity.PublishedByUserId.Value) : null,
            CreatedAt = new CreatedAt(planVersionEntity.CreatedAt)
        };
    }

    public static PlanVersionEntity ToEntity(this PlanVersion planVersion)
    {
        return new PlanVersionEntity
        {
            PlanVersionId = planVersion.PlanVersionId,
            PlanId = planVersion.PlanId,
            VersionNumber = planVersion.VersionNumber,
            CurrencyId = (int)planVersion.Currency,
            CostPerRequest = planVersion.CostPerRequest,
            PlatformFee = planVersion.PlatformFee,
            MinimumCharge = planVersion.MinimumCharge,
            PublishedByUserId = planVersion.PublishedByUserId,
            CreatedAt = planVersion.CreatedAt
        };
    }
}
