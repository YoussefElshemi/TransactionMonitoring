using Core.Factories;
using Core.Models.Plans;
using Core.ValueObjects;

namespace Core.Mappers;

public static class PublishPlanVersionRequestMapper
{
    public static PlanVersion ToModel(this PublishPlanVersionRequest request, VersionNumber versionNumber, TimeProvider timeProvider)
    {
        var planVersionId = new PlanVersionId(Guid.NewGuid());

        return new PlanVersion
        {
            PlanVersionId = planVersionId,
            PlanId = request.PlanId,
            VersionNumber = versionNumber,
            Currency = request.Currency,
            CostPerRequest = PlanPriceFactory.HeadlineRate(request.CostPerRequest, request.Prices),
            PlatformFee = request.PlatformFee,
            MinimumCharge = request.MinimumCharge,
            Prices = PlanPriceFactory.Create(planVersionId, request.CostPerRequest, request.Prices, timeProvider),
            PublishedByUserId = request.PublishedByUserId,
            CreatedAt = new CreatedAt(timeProvider.GetUtcNow())
        };
    }
}
