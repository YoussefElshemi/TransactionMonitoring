using Core.Models.Roles;
using Core.ValueObjects;
using Presentation.Models.Roles;

namespace Presentation.Mappers.Roles;

public static class ListRoleRequestDtoMapper
{
    public static ListRoleRequest ToModel(this ListRoleRequestDto listRoleRequestDto)
    {
        return new ListRoleRequest
        {
            OrganizationId = new OrganizationId(listRoleRequestDto.OrganizationId),
            Name = !string.IsNullOrWhiteSpace(listRoleRequestDto.Name) ? new RoleName(listRoleRequestDto.Name) : null,
            IsBuiltIn = listRoleRequestDto.IsBuiltIn.HasValue ? new IsBuiltIn(listRoleRequestDto.IsBuiltIn.Value) : null,
            PageNumber = new PageNumber(listRoleRequestDto.PageNumber),
            PageSize = new PageSize(listRoleRequestDto.PageSize),
            FromDate = new DateRange(listRoleRequestDto.FromDate),
            ToDate = new DateRange(listRoleRequestDto.ToDate)
        };
    }

    public static ListRoleAssignmentRequest ToModel(this ListRoleAssignmentRequestDto listRoleAssignmentRequestDto)
    {
        return new ListRoleAssignmentRequest
        {
            OrganizationId = new OrganizationId(listRoleAssignmentRequestDto.OrganizationId),
            UserId = listRoleAssignmentRequestDto.UserId.HasValue ? new ApplicationUserId(listRoleAssignmentRequestDto.UserId.Value) : null,
            RoleId = listRoleAssignmentRequestDto.RoleId.HasValue ? new RoleId(listRoleAssignmentRequestDto.RoleId.Value) : null,
            PageNumber = new PageNumber(listRoleAssignmentRequestDto.PageNumber),
            PageSize = new PageSize(listRoleAssignmentRequestDto.PageSize),
            FromDate = new DateRange(listRoleAssignmentRequestDto.FromDate),
            ToDate = new DateRange(listRoleAssignmentRequestDto.ToDate)
        };
    }
}
