using Core.Models.Usages;
using Core.ValueObjects;
using Presentation.Models.Usages;

namespace Presentation.Mappers.Usages;

public static class ListUsageRequestMapper
{
    public static ListUsageRequest ToModel(this ListUsageRequestDto listUsageRequestDto)
    {
        return new ListUsageRequest
        {
            OrganizationId = new OrganizationId(listUsageRequestDto.OrganizationId),
            FromDate = new DateRange(listUsageRequestDto.FromDate),
            ToDate = new DateRange(listUsageRequestDto.ToDate),
            Resource = !string.IsNullOrWhiteSpace(listUsageRequestDto.Resource) ? new Resource(listUsageRequestDto.Resource) : null,
            PageNumber = new PageNumber(listUsageRequestDto.PageNumber),
            PageSize = new PageSize(listUsageRequestDto.PageSize)
        };
    }
}