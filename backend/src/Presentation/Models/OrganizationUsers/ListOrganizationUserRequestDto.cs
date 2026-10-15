using Microsoft.AspNetCore.Mvc;
using Presentation.Models.Common;

namespace Presentation.Models.OrganizationUsers;

public record ListOrganizationUserRequestDto : PagedRequestDto
{
    public string? UserName { get; init; }

    [FromRoute]
    public new required Guid OrganizationId
    {
        get => base.OrganizationId;
        init => base.OrganizationId = value;
    }
}