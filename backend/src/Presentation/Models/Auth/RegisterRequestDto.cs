namespace Presentation.Models.Auth;

public record RegisterRequestDto
{
    public required string Email { get; init; }
    public required string UserName { get; init; }
    public required string Password { get; init; }
}