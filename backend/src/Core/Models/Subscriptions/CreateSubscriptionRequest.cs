using Core.ValueObjects;

namespace Core.Models.Subscriptions;

public record CreateSubscriptionRequest
{
    public required OrganizationId OrganizationId { get; init; }
    public required PlanVersionId PlanVersionId { get; init; }

    public required EffectiveFrom? EffectiveFrom { get; init; }

    public required ApplicationUserId? CreatedByUserId { get; init; }
}
