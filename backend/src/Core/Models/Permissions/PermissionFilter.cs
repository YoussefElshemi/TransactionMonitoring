using Core.ValueObjects;

namespace Core.Models.Permissions;

public record PermissionFilter
{
    public required List<QueueId> QueueIds { get; init; }
    public required List<RuleSetId> RuleSetIds { get; init; }

    public required List<SubOrganizationId> SubOrganizationIds { get; init; }
}
