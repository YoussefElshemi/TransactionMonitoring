namespace Presentation.Models.Revenue;

public record ServiceCostRateResponseDto
{
    public required Guid ServiceCostRateId { get; init; }
    public required string BillableResource { get; init; }
    public required decimal UnitCost { get; init; }
    public required string Currency { get; init; }
    public required DateTimeOffset EffectiveFrom { get; init; }
    public required Guid? CreatedByUserId { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}
