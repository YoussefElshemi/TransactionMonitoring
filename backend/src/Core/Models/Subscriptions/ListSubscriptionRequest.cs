using Core.Models.Common;
using Core.ValueObjects;

namespace Core.Models.Subscriptions;

public record ListSubscriptionRequest : PagedRequest
{
    public required OrganizationId OrganizationId { get; init; }

    public required IncludeSuperseded IncludeSuperseded { get; init; }
}
