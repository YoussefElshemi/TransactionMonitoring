using Core.Models.Subscriptions;
using Core.ValueObjects;

namespace Core.Mappers;

public static class CreateSubscriptionRequestMapper
{
    public static Subscription ToModel(this CreateSubscriptionRequest request, TimeProvider timeProvider)
    {
        var now = timeProvider.GetUtcNow();

        return new Subscription
        {
            SubscriptionId = new SubscriptionId(Guid.NewGuid()),
            OrganizationId = request.OrganizationId,
            PlanVersionId = request.PlanVersionId,
            PlanVersion = null,
            Plan = null,
            EffectiveFrom = request.EffectiveFrom ?? new EffectiveFrom(now),
            EffectiveTo = null,
            CreatedByUserId = request.CreatedByUserId,
            CreatedAt = new CreatedAt(now),
            UpdatedAt = new UpdatedAt(now),
            IsDeleted = new IsDeleted(false),
            DeletedAt = null
        };
    }
}
