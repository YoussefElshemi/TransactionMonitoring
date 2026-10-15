using Microsoft.AspNetCore.Mvc;

namespace Presentation.Models.ApiClients;

public record CreateApiClientSecretRequestDto : ApiClientRequestDto
{
    [FromBody]
    public required CreateApiClientSecretRequestBodyDto Body { get; init; }
}

public record CreateApiClientSecretRequestBodyDto
{
    public DateTimeOffset? ExpiresAt { get; init; }
}
