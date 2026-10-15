namespace Presentation.Models.Users;

public record RemoveUserRoleRequestDto : UserRequestDto
{
    public required string Role { get; init; }
}