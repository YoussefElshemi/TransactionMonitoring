using Microsoft.AspNetCore.Mvc;
using Presentation.Models.Common;
using Presentation.Models.Organizations;

namespace Presentation.Models.Usages;

public record OrganizationUsageRequestDto : OrganizationRequestDto
{
    [FromRoute]
    public required Guid UsageId { get; init; }
}

public record OrganizationListUsageRequestDto : PagedRequestDto
{
    [FromRoute]
    public new required Guid OrganizationId
    {
        get => base.OrganizationId;
        init => base.OrganizationId = value;
    }

    public string? Resource { get; init; }
}
