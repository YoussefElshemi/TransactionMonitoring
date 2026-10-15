namespace Presentation.Models.Users;

public record AddUserRoleRequestDto : UserRequestDto
{
    public required string Role { get; init; }
}