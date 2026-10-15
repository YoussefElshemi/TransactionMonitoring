using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.Discounts;

public record CreateContractDiscountRequest
{
    public required OrganizationId OrganizationId { get; init; }
    public required DiscountType DiscountType { get; init; }
    public required DiscountValue Value { get; init; }
    public required ContractDiscountDescription Description { get; init; }

    public required EffectiveFrom? EffectiveFrom { get; init; }

    public required EffectiveTo? EffectiveTo { get; init; }
    public required ApplicationUserId? CreatedByUserId { get; init; }
}
