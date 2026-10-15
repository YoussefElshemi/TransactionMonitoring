namespace Presentation.Models.Permissions;

public record PermissionResponseDto
{
    public required string Name { get; init; }

    public required string Resource { get; init; }
    public required string Action { get; init; }

    public required bool IsGrantable { get; init; }

    public required bool IsHeld { get; init; }
}
