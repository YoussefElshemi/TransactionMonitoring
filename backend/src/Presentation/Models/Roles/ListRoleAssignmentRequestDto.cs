using Microsoft.AspNetCore.Mvc;
using Presentation.Models.Common;

namespace Presentation.Models.Roles;

public record ListRoleAssignmentRequestDto : PagedRequestDto
{
    public Guid? UserId { get; set; }
    public Guid? RoleId { get; init; }
}

public record OrganizationListRoleAssignmentRequestDto : ListRoleAssignmentRequestDto
{
    [FromRoute]
    public new required Guid OrganizationId
    {
        get => base.OrganizationId;
        init => base.OrganizationId = value;
    }
}
