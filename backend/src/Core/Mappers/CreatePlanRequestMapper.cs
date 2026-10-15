using Core.Factories;
using Core.Models.Plans;
using Core.ValueObjects;

namespace Core.Mappers;

public static class CreatePlanRequestMapper
{
    public static Plan ToModel(this CreatePlanRequest request, PlanId planId, TimeProvider timeProvider)
    {
        var now = timeProvider.GetUtcNow();

        return new Plan
        {
            PlanId = planId,
            Name = request.Name,
            Description = request.Description,
            OrganizationId = request.OrganizationId,
            IsEnabled = new IsEnabled(true),

            CurrentVersionNumber = new VersionNumber(1),
            CurrentVersion = null,
            CreatedAt = new CreatedAt(now),
            UpdatedAt = new UpdatedAt(now),
            IsDeleted = new IsDeleted(false),
            DeletedAt = null
        };
    }

    public static PlanVersion ToVersionModel(this CreatePlanRequest request, PlanId planId, TimeProvider timeProvider)
    {
        var planVersionId = new PlanVersionId(Guid.NewGuid());

        return new PlanVersion
        {
            PlanVersionId = planVersionId,
            PlanId = planId,
            VersionNumber = new VersionNumber(1),
            Currency = request.Currency,
            CostPerRequest = PlanPriceFactory.HeadlineRate(request.CostPerRequest, request.Prices),
            PlatformFee = request.PlatformFee,
            MinimumCharge = request.MinimumCharge,
            Prices = PlanPriceFactory.Create(planVersionId, request.CostPerRequest, request.Prices, timeProvider),
            PublishedByUserId = request.CreatedByUserId,
            CreatedAt = new CreatedAt(timeProvider.GetUtcNow())
        };
    }
}
