using Core.Models.Discounts;
using Core.ValueObjects;
using Presentation.Models.Discounts;

namespace Presentation.Mappers.Discounts;

public static class ContractDiscountMapper
{
    public static CreateContractDiscountRequest ToModel(this CreateContractDiscountRequestDto dto)
    {
        return new CreateContractDiscountRequest
        {
            OrganizationId = new OrganizationId(dto.OrganizationId),
            DiscountType = dto.Body.DiscountType,
            Value = new DiscountValue(dto.Body.Value),
            Description = new ContractDiscountDescription(dto.Body.Description),
            EffectiveFrom = dto.Body.EffectiveFrom.HasValue ? new EffectiveFrom(dto.Body.EffectiveFrom.Value) : null,
            EffectiveTo = dto.Body.EffectiveTo.HasValue ? new EffectiveTo(dto.Body.EffectiveTo.Value) : null,
            CreatedByUserId = dto.CreatedByUserId.HasValue ? new ApplicationUserId(dto.CreatedByUserId.Value) : null
        };
    }

    public static ListContractDiscountRequest ToModel(this ListContractDiscountRequestDto dto)
    {
        return new ListContractDiscountRequest
        {
            OrganizationId = new OrganizationId(dto.OrganizationId),
            IncludeExpired = new IncludeExpired(dto.IncludeExpired ?? false),
            PageNumber = new PageNumber(dto.PageNumber),
            PageSize = new PageSize(dto.PageSize),
            FromDate = new DateRange(dto.FromDate),
            ToDate = new DateRange(dto.ToDate)
        };
    }

    public static ContractDiscountResponseDto ToDto(this ContractDiscount contractDiscount)
    {
        return new ContractDiscountResponseDto
        {
            ContractDiscountId = contractDiscount.ContractDiscountId,
            OrganizationId = contractDiscount.OrganizationId,
            DiscountType = contractDiscount.DiscountType.ToString(),
            Value = contractDiscount.Value,
            Description = contractDiscount.Description,
            EffectiveFrom = contractDiscount.EffectiveFrom,
            EffectiveTo = contractDiscount.EffectiveTo.HasValue ? contractDiscount.EffectiveTo.Value : null,
            CreatedByUserId = contractDiscount.CreatedByUserId.HasValue ? contractDiscount.CreatedByUserId.Value : null,
            CreatedAt = contractDiscount.CreatedAt,
            UpdatedAt = contractDiscount.UpdatedAt
        };
    }
}
