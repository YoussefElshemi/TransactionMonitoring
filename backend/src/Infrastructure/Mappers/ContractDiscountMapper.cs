using Core.Enums;
using Core.Models.Discounts;
using Core.ValueObjects;
using Infrastructure.Entities;

namespace Infrastructure.Mappers;

public static class ContractDiscountMapper
{
    public static ContractDiscount ToModel(this ContractDiscountEntity contractDiscountEntity)
    {
        return new ContractDiscount
        {
            ContractDiscountId = new ContractDiscountId(contractDiscountEntity.ContractDiscountId),
            OrganizationId = new OrganizationId(contractDiscountEntity.OrganizationId),
            DiscountType = (DiscountType)contractDiscountEntity.DiscountTypeId,
            Value = new DiscountValue(contractDiscountEntity.Value),
            Description = new ContractDiscountDescription(contractDiscountEntity.Description),
            EffectiveFrom = new EffectiveFrom(contractDiscountEntity.EffectiveFrom),
            EffectiveTo = contractDiscountEntity.EffectiveTo.HasValue ? new EffectiveTo(contractDiscountEntity.EffectiveTo.Value) : null,
            CreatedByUserId = contractDiscountEntity.CreatedByUserId.HasValue ? new ApplicationUserId(contractDiscountEntity.CreatedByUserId.Value) : null,
            CreatedAt = new CreatedAt(contractDiscountEntity.CreatedAt),
            UpdatedAt = new UpdatedAt(contractDiscountEntity.UpdatedAt),
            IsDeleted = new IsDeleted(contractDiscountEntity.IsDeleted),
            DeletedAt = contractDiscountEntity.DeletedAt.HasValue ? new DeletedAt(contractDiscountEntity.DeletedAt.Value) : null
        };
    }

    public static ContractDiscountEntity ToEntity(this ContractDiscount contractDiscount)
    {
        return new ContractDiscountEntity
        {
            ContractDiscountId = contractDiscount.ContractDiscountId,
            OrganizationId = contractDiscount.OrganizationId,
            DiscountTypeId = (int)contractDiscount.DiscountType,
            Value = contractDiscount.Value,
            Description = contractDiscount.Description,
            EffectiveFrom = contractDiscount.EffectiveFrom,
            EffectiveTo = contractDiscount.EffectiveTo.HasValue ? contractDiscount.EffectiveTo.Value : null,
            CreatedByUserId = contractDiscount.CreatedByUserId.HasValue ? contractDiscount.CreatedByUserId.Value : null,
            CreatedAt = contractDiscount.CreatedAt,
            UpdatedAt = contractDiscount.UpdatedAt,
            IsDeleted = contractDiscount.IsDeleted,
            DeletedAt = contractDiscount.DeletedAt
        };
    }
}
