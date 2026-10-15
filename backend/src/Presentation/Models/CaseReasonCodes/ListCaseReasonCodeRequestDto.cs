using Microsoft.AspNetCore.Mvc;
using Presentation.Models.Common;

namespace Presentation.Models.CaseReasonCodes;

public record ListCaseReasonCodeRequestDto : PagedRequestDto
{
    [FromRoute]
    public new required Guid OrganizationId
    {
        get => base.OrganizationId;
        init => base.OrganizationId = value;
    }

    public string? Code { get; init; }
    public string? Disposition { get; init; }
    public bool? IsEnabled { get; init; }
}
