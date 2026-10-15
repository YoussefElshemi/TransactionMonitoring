using Core.Models.Revenue;
using Core.ValueObjects;

namespace Core.Mappers;

public static class CreateServiceCostRateRequestMapper
{
    public static ServiceCostRate ToModel(this CreateServiceCostRateRequest request, TimeProvider timeProvider)
    {
        var now = timeProvider.GetUtcNow();

        return new ServiceCostRate
        {
            ServiceCostRateId = new ServiceCostRateId(Guid.NewGuid()),
            BillableResource = request.BillableResource,
            UnitCost = request.UnitCost,
            Currency = request.Currency,
            EffectiveFrom = request.EffectiveFrom ?? new EffectiveFrom(now),
            CreatedByUserId = request.CreatedByUserId,
            CreatedAt = new CreatedAt(now),
            UpdatedAt = new UpdatedAt(now)
        };
    }
}
