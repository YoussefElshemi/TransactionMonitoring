using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.Plans;

public record PlanPrice
{
    public required PlanPriceId PlanPriceId { get; init; }
    public required PlanVersionId PlanVersionId { get; init; }

    public required BillableResource? BillableResource { get; init; }

    public required PricingModel PricingModel { get; init; }

    public required FreeAllowance FreeAllowance { get; init; }

    public required List<PlanPriceTier> Tiers { get; init; }
    public required CreatedAt CreatedAt { get; init; }
}
