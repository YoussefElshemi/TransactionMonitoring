namespace Presentation.Models.Users;

public record UserResponseDto
{
    public required Guid UserId { get; init; }
    public required string[] Roles { get; init; }
    public required bool IsEnabled { get; init; }
    public required Guid OrganizationId { get; init; }

    public required Guid? SubOrganizationId { get; init; }
    public required string? Email { get; init; }
    public required string? UserName { get; init; }
    public required bool IsEmailConfirmed { get; init; }

    public required string? Locale { get; init; }
    public required string? TimeZone { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
}
