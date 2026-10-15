using Microsoft.AspNetCore.Mvc;

namespace Presentation.Models.Status;

public record ConfirmStatusSubscriptionRequestDto
{
    [FromRoute]
    public required Guid StatusSubscriptionId { get; init; }

    [FromQuery]
    public required string Token { get; init; }
}
