namespace Presentation.Models.Permissions;

public record PermissionScopeDto
{
    public required string ScopeType { get; init; }
    public Guid? SubOrganizationId { get; init; }
    public Guid? RuleSetId { get; init; }
    public Guid? QueueId { get; init; }
}
