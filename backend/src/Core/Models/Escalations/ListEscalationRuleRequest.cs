using Core.Models.Common;
using Core.ValueObjects;

namespace Core.Models.Escalations;

public record ListEscalationRuleRequest : PagedRequest
{
    public required OrganizationId OrganizationId { get; init; }
    public required EscalationRuleName? Name { get; init; }
    public required IsEnabled? IsEnabled { get; init; }
}
