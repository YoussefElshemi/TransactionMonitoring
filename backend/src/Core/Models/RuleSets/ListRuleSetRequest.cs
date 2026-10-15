using Core.Models.Common;
using Core.Models.Permissions;
using Core.ValueObjects;

namespace Core.Models.RuleSets;

public record ListRuleSetRequest : PagedRequest
{
    public required OrganizationId OrganizationId { get; init; }
    public required FriendlyName? FriendlyName { get; init; }

    public PermissionFilter? PermissionFilter { get; init; }
}