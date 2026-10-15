using Microsoft.AspNetCore.Mvc;

namespace Presentation.Models.Roles;

public record UpdateRoleRequestDto : OrganizationRoleRequestDto
{
    [FromBody]
    public required UpdateRoleRequestBodyDto Body { get; init; }
}

public record UpdateRoleRequestBodyDto
{
    public string? Name { get; init; }
    public string? Description { get; init; }

    public string[]? Permissions { get; init; }
}
