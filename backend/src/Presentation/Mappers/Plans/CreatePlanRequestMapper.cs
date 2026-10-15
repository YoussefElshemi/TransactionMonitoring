using Core.Models.Plans;
using Core.ValueObjects;
using Presentation.Models.Plans;

namespace Presentation.Mappers.Plans;

public static class CreatePlanRequestMapper
{
    public static CreatePlanRequest ToModel(this CreatePlanRequestDto dto)
    {
        return new CreatePlanRequest
        {
            Name = new PlanName(dto.Body.Name),
            Description = !string.IsNullOrWhiteSpace(dto.Body.Description) ? new PlanDescription(dto.Body.Description) : null,
            OrganizationId = dto.Body.OrganizationId.HasValue ? new OrganizationId(dto.Body.OrganizationId.Value) : null,
            CostPerRequest = new CostPerRequest(dto.Body.CostPerRequest),
            Currency = dto.Body.Currency,
            PlatformFee = new PlatformFee(dto.Body.PlatformFee),
            MinimumCharge = new MinimumCharge(dto.Body.MinimumCharge),
            Prices = dto.Body.Prices.ToModel(),
            CreatedByUserId = dto.CreatedByUserId.HasValue ? new ApplicationUserId(dto.CreatedByUserId.Value) : null
        };
    }
}
