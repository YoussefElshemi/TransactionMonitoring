using Core.Models.Subscriptions;
using Core.ValueObjects;
using Presentation.Models.Subscriptions;

namespace Presentation.Mappers.Subscriptions;

public static class CreateSubscriptionRequestMapper
{
    public static CreateSubscriptionRequest ToModel(this CreateSubscriptionRequestDto dto)
    {
        return new CreateSubscriptionRequest
        {
            OrganizationId = new OrganizationId(dto.OrganizationId),
            PlanVersionId = new PlanVersionId(dto.Body.PlanVersionId),
            EffectiveFrom = dto.Body.EffectiveFrom.HasValue ? new EffectiveFrom(dto.Body.EffectiveFrom.Value) : null,
            CreatedByUserId = dto.CreatedByUserId.HasValue ? new ApplicationUserId(dto.CreatedByUserId.Value) : null
        };
    }
}
