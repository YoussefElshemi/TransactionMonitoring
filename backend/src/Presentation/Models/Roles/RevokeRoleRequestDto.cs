using Microsoft.AspNetCore.Mvc;
using Presentation.Models.Organizations;

namespace Presentation.Models.Roles;

public record RevokeRoleRequestDto : OrganizationRequestDto
{
    [FromRoute]
    public required Guid RoleAssignmentId { get; init; }
}
