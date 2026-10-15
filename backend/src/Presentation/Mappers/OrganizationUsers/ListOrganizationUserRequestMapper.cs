using Core.Models.Organizations;
using Core.ValueObjects;
using Presentation.Models.OrganizationUsers;

namespace Presentation.Mappers.OrganizationUsers;

public static class ListOrganizationUserRequestMapper
{
    public static ListOrganizationUserRequest ToModel(this ListOrganizationUserRequestDto listOrganizationRequestDto)
    {
        return new ListOrganizationUserRequest
        {
            OrganizationId = new OrganizationId(listOrganizationRequestDto.OrganizationId),
            UserName = !string.IsNullOrWhiteSpace(listOrganizationRequestDto.UserName) ? new UserName(listOrganizationRequestDto.UserName) : null,
            PageNumber = new PageNumber(listOrganizationRequestDto.PageNumber),
            PageSize = new PageSize(listOrganizationRequestDto.PageSize),
            FromDate = new DateRange(listOrganizationRequestDto.FromDate),
            ToDate = new DateRange(listOrganizationRequestDto.ToDate)
        };
    }
}