using Core.Models.Plans;
using Core.ValueObjects;
using Presentation.Models.Plans;

namespace Presentation.Mappers.Plans;

public static class PublishPlanVersionRequestMapper
{
    public static PublishPlanVersionRequest ToModel(this PublishPlanVersionRequestDto dto)
    {
        return new PublishPlanVersionRequest
        {
            PlanId = new PlanId(dto.PlanId),
            CostPerRequest = new CostPerRequest(dto.Body.CostPerRequest),
            Currency = dto.Body.Currency,
            PlatformFee = new PlatformFee(dto.Body.PlatformFee),
            MinimumCharge = new MinimumCharge(dto.Body.MinimumCharge),
            Prices = dto.Body.Prices.ToModel(),
            PublishedByUserId = dto.PublishedByUserId.HasValue ? new ApplicationUserId(dto.PublishedByUserId.Value) : null
        };
    }
}
