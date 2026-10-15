namespace Presentation.Models.Plans;

public record PlanPriceResponseDto
{
    public required Guid PlanPriceId { get; init; }
    public required Guid PlanVersionId { get; init; }
    public required string? BillableResource { get; init; }
    public required string PricingModel { get; init; }
    public required int FreeAllowance { get; init; }
    public required List<PlanPriceTierResponseDto> Tiers { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}

public record PlanPriceTierResponseDto
{
    public required Guid PlanPriceTierId { get; init; }
    public required int FromQuantity { get; init; }
    public required int? ToQuantity { get; init; }
    public required decimal UnitAmount { get; init; }
    public required decimal FlatAmount { get; init; }
}
