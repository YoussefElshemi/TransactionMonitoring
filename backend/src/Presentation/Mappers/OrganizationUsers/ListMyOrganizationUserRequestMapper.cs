using Core.Models.Organizations;
using Core.ValueObjects;
using Presentation.Models.OrganizationUsers;

namespace Presentation.Mappers.OrganizationUsers;

public static class ListMyOrganizationUserRequestMapper
{
    public static ListOrganizationUserRequest ToModel(this ListMyOrganizationUserRequestDto listMyOrganizationUserRequestDto)
    {
        return new ListOrganizationUserRequest
        {
            OrganizationId = new OrganizationId(listMyOrganizationUserRequestDto.OrganizationId),
            UserName = !string.IsNullOrWhiteSpace(listMyOrganizationUserRequestDto.UserName) ? new UserName(listMyOrganizationUserRequestDto.UserName) : null,
            PageNumber = new PageNumber(listMyOrganizationUserRequestDto.PageNumber),
            PageSize = new PageSize(listMyOrganizationUserRequestDto.PageSize),
            FromDate = new DateRange(listMyOrganizationUserRequestDto.FromDate),
            ToDate = new DateRange(listMyOrganizationUserRequestDto.ToDate)
        };
    }
}
