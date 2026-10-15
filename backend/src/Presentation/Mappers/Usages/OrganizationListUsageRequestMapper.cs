using Core.Models.Usages;
using Core.ValueObjects;
using Presentation.Models.Usages;

namespace Presentation.Mappers.Usages;

public static class OrganizationListUsageRequestMapper
{
    public static ListUsageRequest ToModel(this OrganizationListUsageRequestDto organizationListUsageRequestDto)
    {
        return new ListUsageRequest
        {
            OrganizationId = new OrganizationId(organizationListUsageRequestDto.OrganizationId),
            FromDate = new DateRange(organizationListUsageRequestDto.FromDate),
            ToDate = new DateRange(organizationListUsageRequestDto.ToDate),
            Resource = !string.IsNullOrWhiteSpace(organizationListUsageRequestDto.Resource)
                ? new Resource(organizationListUsageRequestDto.Resource)
                : null,
            PageNumber = new PageNumber(organizationListUsageRequestDto.PageNumber),
            PageSize = new PageSize(organizationListUsageRequestDto.PageSize)
        };
    }
}
