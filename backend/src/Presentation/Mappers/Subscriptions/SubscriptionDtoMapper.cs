using Core.Models.Subscriptions;
using Presentation.Mappers.Plans;
using Presentation.Models.Subscriptions;

namespace Presentation.Mappers.Subscriptions;

public static class SubscriptionDtoMapper
{
    public static SubscriptionResponseDto ToDto(this Subscription subscription)
    {
        return new SubscriptionResponseDto
        {
            SubscriptionId = subscription.SubscriptionId,
            OrganizationId = subscription.OrganizationId,
            PlanVersionId = subscription.PlanVersionId,
            PlanVersion = subscription.PlanVersion?.ToDto(),
            PlanName = subscription.Plan?.Name.ToString(),
            EffectiveFrom = subscription.EffectiveFrom,
            EffectiveTo = subscription.EffectiveTo.HasValue ? subscription.EffectiveTo.Value : null,
            CreatedByUserId = subscription.CreatedByUserId.HasValue ? subscription.CreatedByUserId.Value : null,
            CreatedAt = subscription.CreatedAt,
            UpdatedAt = subscription.UpdatedAt
        };
    }
}
