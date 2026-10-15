using Microsoft.AspNetCore.Mvc;
using Presentation.Models.Organizations;

namespace Presentation.Models.CaseReasonCodes;

public record OrganizationCaseReasonCodeRequestDto : OrganizationRequestDto
{
    [FromRoute]
    public required Guid CaseReasonCodeId { get; init; }
}
