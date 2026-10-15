using Core.Enums;

namespace Presentation.Models.Plans;

public record PlanPriceRequestDto
{
    public BillableResource? BillableResource { get; init; }

    public required PricingModel PricingModel { get; init; }
    public int FreeAllowance { get; init; }
    public required List<PlanPriceTierRequestDto> Tiers { get; init; }
}

public record PlanPriceTierRequestDto
{
    public required int FromQuantity { get; init; }
    public int? ToQuantity { get; init; }

    public required decimal UnitAmount { get; init; }

    public decimal FlatAmount { get; init; }
}
