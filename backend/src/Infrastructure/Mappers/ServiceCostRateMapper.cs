using Core.Enums;
using Core.Models.Revenue;
using Core.ValueObjects;
using Infrastructure.Entities;

namespace Infrastructure.Mappers;

public static class ServiceCostRateMapper
{
    public static ServiceCostRate ToModel(this ServiceCostRateEntity serviceCostRateEntity)
    {
        return new ServiceCostRate
        {
            ServiceCostRateId = new ServiceCostRateId(serviceCostRateEntity.ServiceCostRateId),
            BillableResource = (BillableResource)serviceCostRateEntity.BillableResourceId,
            UnitCost = new UnitCost(serviceCostRateEntity.UnitCost),
            Currency = (Currency)serviceCostRateEntity.CurrencyId,
            EffectiveFrom = new EffectiveFrom(serviceCostRateEntity.EffectiveFrom),
            CreatedByUserId = serviceCostRateEntity.CreatedByUserId.HasValue ? new ApplicationUserId(serviceCostRateEntity.CreatedByUserId.Value) : null,
            CreatedAt = new CreatedAt(serviceCostRateEntity.CreatedAt),
            UpdatedAt = new UpdatedAt(serviceCostRateEntity.UpdatedAt)
        };
    }

    public static ServiceCostRateEntity ToEntity(this ServiceCostRate serviceCostRate)
    {
        return new ServiceCostRateEntity
        {
            ServiceCostRateId = serviceCostRate.ServiceCostRateId,
            BillableResourceId = (int)serviceCostRate.BillableResource,
            UnitCost = serviceCostRate.UnitCost,
            CurrencyId = (int)serviceCostRate.Currency,
            EffectiveFrom = serviceCostRate.EffectiveFrom,
            CreatedByUserId = serviceCostRate.CreatedByUserId.HasValue ? serviceCostRate.CreatedByUserId.Value : null,
            CreatedAt = serviceCostRate.CreatedAt,
            UpdatedAt = serviceCostRate.UpdatedAt
        };
    }
}
