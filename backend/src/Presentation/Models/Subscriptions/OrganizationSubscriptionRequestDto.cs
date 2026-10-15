using Microsoft.AspNetCore.Mvc;

namespace Presentation.Models.Subscriptions;

public record OrganizationSubscriptionRequestDto
{
    [FromRoute]
    public required Guid OrganizationId { get; init; }

    [FromRoute]
    public required Guid SubscriptionId { get; init; }
}
