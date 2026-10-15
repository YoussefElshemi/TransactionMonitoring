using Core.ValueObjects;

namespace Core.Models.SubOrganizations;

public record SubOrganizationUsage
{
    public required SubOrganizationId SubOrganizationId { get; init; }
    public required SubOrganizationName Name { get; init; }
    public required TotalCount UserCount { get; init; }
    public required TotalCount QueueCount { get; init; }
    public required TotalCount RuleSetCount { get; init; }
    public required TotalCount RoleAssignmentCount { get; init; }
}
