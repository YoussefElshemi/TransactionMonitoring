namespace Infrastructure.Entities;

public record PlanPriceTierEntity
{
    public required Guid PlanPriceTierId { get; init; }
    public required Guid PlanPriceId { get; init; }
    public PlanPriceEntity PlanPrice { get; init; } = null!;
    public required int FromQuantity { get; init; }

    public required int? ToQuantity { get; init; }

    public required decimal UnitAmount { get; init; }
    public required decimal FlatAmount { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}
