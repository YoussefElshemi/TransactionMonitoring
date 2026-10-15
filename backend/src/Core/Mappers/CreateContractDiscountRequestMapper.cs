using Core.Models.Discounts;
using Core.ValueObjects;

namespace Core.Mappers;

public static class CreateContractDiscountRequestMapper
{
    public static ContractDiscount ToModel(this CreateContractDiscountRequest request, TimeProvider timeProvider)
    {
        var now = timeProvider.GetUtcNow();

        return new ContractDiscount
        {
            ContractDiscountId = new ContractDiscountId(Guid.NewGuid()),
            OrganizationId = request.OrganizationId,
            DiscountType = request.DiscountType,
            Value = request.Value,
            Description = request.Description,
            EffectiveFrom = request.EffectiveFrom ?? new EffectiveFrom(now),
            EffectiveTo = request.EffectiveTo,
            CreatedByUserId = request.CreatedByUserId,
            CreatedAt = new CreatedAt(now),
            UpdatedAt = new UpdatedAt(now),
            IsDeleted = new IsDeleted(false),
            DeletedAt = null
        };
    }
}
