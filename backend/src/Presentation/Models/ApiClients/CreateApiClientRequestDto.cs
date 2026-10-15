namespace Presentation.Models.ApiClients;

public record CreateApiClientRequestDto
{
    public required string FriendlyName { get; init; }

    public required string[] Scopes { get; init; }
}
