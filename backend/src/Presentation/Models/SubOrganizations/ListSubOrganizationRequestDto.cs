using Microsoft.AspNetCore.Mvc;
using Presentation.Models.Common;

namespace Presentation.Models.SubOrganizations;

public record ListSubOrganizationRequestDto : PagedRequestDto
{
    public string? Name { get; init; }
    public bool? IsEnabled { get; init; }
}

public record OrganizationListSubOrganizationRequestDto : ListSubOrganizationRequestDto
{
    [FromRoute]
    public new required Guid OrganizationId
    {
        get => base.OrganizationId;
        init => base.OrganizationId = value;
    }
}
