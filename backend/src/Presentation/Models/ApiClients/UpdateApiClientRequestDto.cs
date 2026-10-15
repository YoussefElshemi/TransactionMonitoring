using Microsoft.AspNetCore.Mvc;

namespace Presentation.Models.ApiClients;

public record UpdateApiClientRequestDto : ApiClientRequestDto
{
    [FromBody]
    public required UpdateApiClientRequestBodyDto Body { get; init; }
}

public record UpdateApiClientRequestBodyDto
{
    public string? FriendlyName { get; init; }
    public bool? IsEnabled { get; init; }

    public string[]? Scopes { get; init; }

    public string? ApiVersion { get; init; }
}
