namespace Presentation.Models.Roles;

public record RoleResponseDto
{
    public required Guid RoleId { get; init; }

    public required Guid? OrganizationId { get; init; }

    public required string Name { get; init; }
    public required string? Description { get; init; }
    public required bool IsBuiltIn { get; init; }
    public required string[] Permissions { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
}
