using Core.Helpers;
using Core.Models.Plans;
using Presentation.Models.Plans;

namespace Presentation.Mappers.Plans;

public static class PlanDtoMapper
{
    public static PlanResponseDto ToDto(this Plan plan)
    {
        return new PlanResponseDto
        {
            PlanId = plan.PlanId,
            Name = plan.Name,
            Description = plan.Description?.ToString(),
            OrganizationId = plan.OrganizationId.HasValue ? plan.OrganizationId.Value : null,
            IsEnabled = plan.IsEnabled,
            CurrentVersionNumber = plan.CurrentVersionNumber,
            CurrentVersion = plan.CurrentVersion?.ToDto(),
            CreatedAt = plan.CreatedAt,
            UpdatedAt = plan.UpdatedAt
        };
    }

    public static PlanVersionResponseDto ToDto(this PlanVersion planVersion)
    {
        return new PlanVersionResponseDto
        {
            PlanVersionId = planVersion.PlanVersionId,
            PlanId = planVersion.PlanId,
            VersionNumber = planVersion.VersionNumber,
            Currency = CurrencyFormatter.Code(planVersion.Currency),
            CostPerRequest = planVersion.CostPerRequest,
            PlatformFee = planVersion.PlatformFee,
            MinimumCharge = planVersion.MinimumCharge,
            Prices = planVersion.Prices.ConvertAll(x => x.ToDto()),
            PublishedByUserId = planVersion.PublishedByUserId.HasValue ? planVersion.PublishedByUserId.Value : null,
            CreatedAt = planVersion.CreatedAt
        };
    }
}
