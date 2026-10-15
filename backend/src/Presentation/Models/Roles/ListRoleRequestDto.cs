using Microsoft.AspNetCore.Mvc;
using Presentation.Models.Common;

namespace Presentation.Models.Roles;

public record ListRoleRequestDto : PagedRequestDto
{
    public string? Name { get; init; }
    public bool? IsBuiltIn { get; init; }
}

public record OrganizationListRoleRequestDto : ListRoleRequestDto
{
    [FromRoute]
    public new required Guid OrganizationId
    {
        get => base.OrganizationId;
        init => base.OrganizationId = value;
    }
}
