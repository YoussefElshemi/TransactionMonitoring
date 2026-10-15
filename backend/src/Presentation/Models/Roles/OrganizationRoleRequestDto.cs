using Microsoft.AspNetCore.Mvc;
using Presentation.Models.Organizations;

namespace Presentation.Models.Roles;

public record OrganizationRoleRequestDto : OrganizationRequestDto
{
    [FromRoute]
    public required Guid RoleId { get; init; }
}
