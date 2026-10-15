using Core.Models.Common;
using Core.ValueObjects;

namespace Core.Models.RuleDeployments;

public record ListRuleDeploymentRequest : PagedRequest
{
    public required OrganizationId OrganizationId { get; init; }
    public required RuleId? RuleId { get; init; }
}
