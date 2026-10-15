using Microsoft.AspNetCore.Mvc;

namespace Presentation.Models.ApiClients;

public record ApiClientSecretRequestDto : ApiClientRequestDto
{
    [FromRoute]
    public required Guid ApiClientSecretId { get; init; }
}
