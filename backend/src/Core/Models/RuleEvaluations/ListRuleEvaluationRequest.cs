using Core.Models.Common;
using Core.ValueObjects;

namespace Core.Models.RuleEvaluations;

public record ListRuleEvaluationRequest : PagedRequest
{
    public required OrganizationId OrganizationId { get; init; }
    public required RuleId? RuleId { get; init; }
}