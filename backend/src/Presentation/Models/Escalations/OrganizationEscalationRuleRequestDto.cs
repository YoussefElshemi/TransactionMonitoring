using Microsoft.AspNetCore.Mvc;

namespace Presentation.Models.Escalations;

public record OrganizationEscalationRuleRequestDto
{
    [FromRoute]
    public required Guid OrganizationId { get; init; }

    [FromRoute]
    public required Guid EscalationRuleId { get; init; }
}
