using Microsoft.AspNetCore.Mvc;

namespace Presentation.Models.CaseReasonCodes;

public record CreateCaseReasonCodeRequestDto
{
    [FromRoute]
    public required Guid OrganizationId { get; init; }

    [FromBody]
    public required CreateCaseReasonCodeRequestBodyDto Body { get; init; }
}

public record CreateCaseReasonCodeRequestBodyDto
{
    public required string Code { get; init; }
    public required string Description { get; init; }
    public string? Disposition { get; init; }
    public required bool IsEnabled { get; init; }
}
