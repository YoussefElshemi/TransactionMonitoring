namespace Presentation.Models.OAuth;

public record TokenRequestDto
{
    public string? GrantType { get; init; }

    public string? ClientId { get; init; }
    public string? ClientSecret { get; init; }

    public string? Scope { get; init; }

    public string? RefreshToken { get; init; }
}
