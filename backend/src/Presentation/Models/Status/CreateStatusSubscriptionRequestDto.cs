using Microsoft.AspNetCore.Mvc;

namespace Presentation.Models.Status;

public record CreateStatusSubscriptionRequestDto
{
    [FromBody]
    public required CreateStatusSubscriptionRequestBodyDto Body { get; init; }
}

public record CreateStatusSubscriptionRequestBodyDto
{
    public required string Email { get; init; }
}
