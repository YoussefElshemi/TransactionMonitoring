namespace Presentation.Models.Permissions;

public record EffectivePermissionResponseDto
{
    public required string Name { get; init; }
    public required PermissionScopeDto Scope { get; init; }
}
