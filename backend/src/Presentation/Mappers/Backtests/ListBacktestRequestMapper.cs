using Core.Models.BackTests;
using Core.ValueObjects;
using Presentation.Models.BackTests;

namespace Presentation.Mappers.BackTests;

public static class ListBackTestRequestMapper
{
    public static ListBackTestRequest ToModel(this ListBackTestRequestDto dto)
    {
        return new ListBackTestRequest
        {
            OrganizationId = new OrganizationId(dto.OrganizationId),
            RuleId = dto.RuleId.HasValue ? new RuleId(dto.RuleId.Value) : null,
            PageNumber = new PageNumber(dto.PageNumber),
            PageSize = new PageSize(dto.PageSize),
            FromDate = new DateRange(dto.FromDate),
            ToDate = new DateRange(dto.ToDate)
        };
    }
}
