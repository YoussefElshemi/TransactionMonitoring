using Microsoft.AspNetCore.Mvc;

namespace Presentation.Models.CaseSlaTargets;

public record OrganizationSlaTargetRequestDto
{
    [FromRoute]
    public required Guid OrganizationId { get; init; }
}
