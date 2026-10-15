using Microsoft.AspNetCore.Mvc;
using Presentation.Models.Organizations;

namespace Presentation.Models.Roles;

public record CreateRoleRequestDto : OrganizationRequestDto
{
    [FromBody]
    public required CreateRoleRequestBodyDto Body { get; init; }
}

public record CreateRoleRequestBodyDto
{
    public required string Name { get; init; }
    public string? Description { get; init; }

    public required string[] Permissions { get; init; }
}
