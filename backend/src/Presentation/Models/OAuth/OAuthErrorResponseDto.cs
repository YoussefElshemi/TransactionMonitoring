using System.Text.Json.Serialization;

namespace Presentation.Models.OAuth;

public record OAuthErrorResponseDto
{
    [JsonPropertyName("error")]
    public required string Error { get; init; }

    [JsonPropertyName("error_description")]
    public required string ErrorDescription { get; init; }
}
