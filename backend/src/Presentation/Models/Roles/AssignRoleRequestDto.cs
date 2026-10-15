using Microsoft.AspNetCore.Mvc;
using Presentation.Models.Organizations;

namespace Presentation.Models.Roles;

public record AssignRoleRequestDto : OrganizationRequestDto
{
    [FromRoute]
    public required Guid UserId { get; init; }

    [FromBody]
    public required AssignRoleRequestBodyDto Body { get; init; }
}

public record AssignRoleRequestBodyDto
{
    public required Guid RoleId { get; init; }

    public string? ScopeType { get; init; }

    public Guid? SubOrganizationId { get; init; }
    public Guid? RuleSetId { get; init; }
    public Guid? QueueId { get; init; }
}
