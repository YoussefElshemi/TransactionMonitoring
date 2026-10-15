using Core.Models.Common;
using Core.Models.Permissions;
using Core.ValueObjects;

namespace Core.Models.Rules;

public record ListRuleRequest : PagedRequest
{
    public required OrganizationId OrganizationId { get; init; }
    public required RuleSetId RuleSetId { get; init; }
    public required FriendlyName? FriendlyName { get; init; }

    public PermissionFilter? PermissionFilter { get; init; }
}