using Core.Models.Plans;
using Core.ValueObjects;
using Presentation.Models.Plans;

namespace Presentation.Mappers.Plans;

public static class ListPlanRequestMapper
{
    public static ListPlanRequest ToModel(this ListPlanRequestDto dto)
    {
        return new ListPlanRequest
        {
            Name = !string.IsNullOrWhiteSpace(dto.Name) ? new PlanName(dto.Name) : null,
            OrganizationId = dto.OwnerOrganizationId.HasValue ? new OrganizationId(dto.OwnerOrganizationId.Value) : null,
            IncludeDisabled = new IncludeDisabled(dto.IncludeDisabled ?? false),
            PageNumber = new PageNumber(dto.PageNumber),
            PageSize = new PageSize(dto.PageSize),
            FromDate = new DateRange(dto.FromDate),
            ToDate = new DateRange(dto.ToDate)
        };
    }

    public static ListPlanVersionRequest ToModel(this ListPlanVersionRequestDto dto)
    {
        return new ListPlanVersionRequest
        {
            PlanId = new PlanId(dto.PlanId),
            PageNumber = new PageNumber(dto.PageNumber),
            PageSize = new PageSize(dto.PageSize),
            FromDate = new DateRange(dto.FromDate),
            ToDate = new DateRange(dto.ToDate)
        };
    }
}
