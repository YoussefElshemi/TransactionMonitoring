using Microsoft.AspNetCore.Mvc;

namespace Presentation.Models.CaseReasonCodes;

public record UpdateCaseReasonCodeRequestDto : OrganizationCaseReasonCodeRequestDto
{
    [FromBody]
    public required UpdateCaseReasonCodeRequestBodyDto Body { get; init; }
}

public record UpdateCaseReasonCodeRequestBodyDto
{
    public string? Description { get; init; }
    public string? Disposition { get; init; }
    public bool? ClearDisposition { get; init; }
    public bool? IsEnabled { get; init; }
}
