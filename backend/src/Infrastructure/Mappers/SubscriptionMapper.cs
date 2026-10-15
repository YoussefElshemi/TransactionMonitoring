using Core.Models.Subscriptions;
using Core.ValueObjects;
using Infrastructure.Entities;

namespace Infrastructure.Mappers;

public static class SubscriptionMapper
{
    public static Subscription ToModel(this SubscriptionEntity subscriptionEntity)
    {
        return new Subscription
        {
            SubscriptionId = new SubscriptionId(subscriptionEntity.SubscriptionId),
            OrganizationId = new OrganizationId(subscriptionEntity.OrganizationId),
            PlanVersionId = new PlanVersionId(subscriptionEntity.PlanVersionId),
            PlanVersion = subscriptionEntity.PlanVersion?.ToModel(),
            Plan = subscriptionEntity.PlanVersion?.Plan?.ToModel(),
            EffectiveFrom = new EffectiveFrom(subscriptionEntity.EffectiveFrom),
            EffectiveTo = subscriptionEntity.EffectiveTo.HasValue ? new EffectiveTo(subscriptionEntity.EffectiveTo.Value) : null,
            CreatedByUserId = subscriptionEntity.CreatedByUserId.HasValue ? new ApplicationUserId(subscriptionEntity.CreatedByUserId.Value) : null,
            CreatedAt = new CreatedAt(subscriptionEntity.CreatedAt),
            UpdatedAt = new UpdatedAt(subscriptionEntity.UpdatedAt),
            IsDeleted = new IsDeleted(subscriptionEntity.IsDeleted),
            DeletedAt = subscriptionEntity.DeletedAt.HasValue ? new DeletedAt(subscriptionEntity.DeletedAt.Value) : null
        };
    }

    public static SubscriptionEntity ToEntity(this Subscription subscription)
    {
        return new SubscriptionEntity
        {
            SubscriptionId = subscription.SubscriptionId,
            OrganizationId = subscription.OrganizationId,
            PlanVersionId = subscription.PlanVersionId,
            EffectiveFrom = subscription.EffectiveFrom,
            EffectiveTo = subscription.EffectiveTo,
            CreatedByUserId = subscription.CreatedByUserId,
            CreatedAt = subscription.CreatedAt,
            UpdatedAt = subscription.UpdatedAt,
            IsDeleted = subscription.IsDeleted,
            DeletedAt = subscription.DeletedAt
        };
    }
}
