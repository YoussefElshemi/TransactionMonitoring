namespace Presentation.Models.SubOrganizations;

public record SubOrganizationUsageResponseDto
{
    public required Guid SubOrganizationId { get; init; }
    public required string Name { get; init; }
    public required int UserCount { get; init; }
    public required int QueueCount { get; init; }
    public required int RuleSetCount { get; init; }
    public required int RoleAssignmentCount { get; init; }
}
