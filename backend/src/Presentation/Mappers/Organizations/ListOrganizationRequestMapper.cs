using Core.Models.Organizations;
using Core.ValueObjects;
using Presentation.Models.Organizations;

namespace Presentation.Mappers.Organizations;

public static class ListOrganizationRequestMapper
{
    public static ListOrganizationRequest ToModel(this ListOrganizationRequestDto listOrganizationRequestDto)
    {
        return new ListOrganizationRequest
        {
            OrganizationId = new OrganizationId(listOrganizationRequestDto.OrganizationId),
            FriendlyName = !string.IsNullOrWhiteSpace(listOrganizationRequestDto.FriendlyName) ? new FriendlyName(listOrganizationRequestDto.FriendlyName) : null,
            PageNumber = new PageNumber(listOrganizationRequestDto.PageNumber),
            PageSize = new PageSize(listOrganizationRequestDto.PageSize),
            FromDate = new DateRange(listOrganizationRequestDto.FromDate),
            ToDate = new DateRange(listOrganizationRequestDto.ToDate)
        };
    }
}