using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.Plans;

public record CreatePlanPriceRequest
{
    public required BillableResource? BillableResource { get; init; }

    public required PricingModel PricingModel { get; init; }
    public required FreeAllowance FreeAllowance { get; init; }
    public required List<CreatePlanPriceTierRequest> Tiers { get; init; }
}
