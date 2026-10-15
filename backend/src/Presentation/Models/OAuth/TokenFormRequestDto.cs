using Microsoft.AspNetCore.Mvc;

namespace Presentation.Models.OAuth;

public record TokenFormRequestDto
{
    [FromForm(Name = "grant_type")]
    public string? GrantType { get; init; }

    [FromForm(Name = "client_id")]
    public string? ClientId { get; init; }

    [FromForm(Name = "client_secret")]
    public string? ClientSecret { get; init; }

    [FromForm(Name = "scope")]
    public string? Scope { get; init; }

    [FromForm(Name = "refresh_token")]
    public string? RefreshToken { get; init; }

    public TokenRequestDto ToRequest()
    {
        return new TokenRequestDto
        {
            GrantType = GrantType,
            ClientId = ClientId,
            ClientSecret = ClientSecret,
            Scope = Scope,
            RefreshToken = RefreshToken
        };
    }
}
