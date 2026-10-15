namespace Presentation.Models.Auth;

public record LoginRequestDto
{
    public required string UserName { get; init; }
    public required string Password { get; init; }
    public string? TwoFactorCode { get; init; }
    public string? TwoFactorRecoveryCode { get; init; }
}