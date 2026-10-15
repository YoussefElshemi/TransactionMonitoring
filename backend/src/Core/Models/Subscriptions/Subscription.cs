using Core.Models.BaseModels;
using Core.Models.Plans;
using Core.ValueObjects;

namespace Core.Models.Subscriptions;

public record Subscription : DeletableModel
{
    public required SubscriptionId SubscriptionId { get; init; }
    public required OrganizationId OrganizationId { get; init; }
    public required PlanVersionId PlanVersionId { get; init; }
    public required PlanVersion? PlanVersion { get; init; }
    public required Plan? Plan { get; init; }

    public required EffectiveFrom EffectiveFrom { get; init; }

    public required EffectiveTo? EffectiveTo { get; init; }

    public required ApplicationUserId? CreatedByUserId { get; init; }
}
