using Core.Helpers;
using Core.Models.Revenue;
using Core.ValueObjects;
using Presentation.Models.Revenue;

namespace Presentation.Mappers.Revenue;

public static class ServiceCostRateDtoMapper
{
    public static CreateServiceCostRateRequest ToModel(this CreateServiceCostRateRequestDto dto)
    {
        return new CreateServiceCostRateRequest
        {
            BillableResource = dto.Body.BillableResource,
            UnitCost = new UnitCost(dto.Body.UnitCost),
            Currency = dto.Body.Currency,
            EffectiveFrom = dto.Body.EffectiveFrom.HasValue ? new EffectiveFrom(dto.Body.EffectiveFrom.Value) : null,
            CreatedByUserId = dto.CreatedByUserId.HasValue ? new ApplicationUserId(dto.CreatedByUserId.Value) : null
        };
    }

    public static ListServiceCostRateRequest ToModel(this ListServiceCostRateRequestDto dto)
    {
        return new ListServiceCostRateRequest
        {
            BillableResource = dto.BillableResource,
            PageNumber = new PageNumber(dto.PageNumber),
            PageSize = new PageSize(dto.PageSize),
            FromDate = new DateRange(dto.FromDate),
            ToDate = new DateRange(dto.ToDate)
        };
    }

    public static ServiceCostRateResponseDto ToDto(this ServiceCostRate serviceCostRate)
    {
        return new ServiceCostRateResponseDto
        {
            ServiceCostRateId = serviceCostRate.ServiceCostRateId,
            BillableResource = serviceCostRate.BillableResource.ToString(),
            UnitCost = serviceCostRate.UnitCost,
            Currency = CurrencyFormatter.Code(serviceCostRate.Currency),
            EffectiveFrom = serviceCostRate.EffectiveFrom,
            CreatedByUserId = serviceCostRate.CreatedByUserId.HasValue ? serviceCostRate.CreatedByUserId.Value : null,
            CreatedAt = serviceCostRate.CreatedAt
        };
    }
}
