namespace Presentation.Models.Roles;

public record RoleUsageResponseDto
{
    public required Guid RoleId { get; init; }
    public required string Name { get; init; }
    public required int AssignmentCount { get; init; }
    public required Guid[] UserIds { get; init; }
}
