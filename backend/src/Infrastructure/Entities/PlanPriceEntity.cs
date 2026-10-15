namespace Infrastructure.Entities;

public record PlanPriceEntity
{
    public required Guid PlanPriceId { get; init; }
    public required Guid PlanVersionId { get; init; }
    public PlanVersionEntity PlanVersion { get; init; } = null!;

    public required int? BillableResourceId { get; init; }
    public BillableResourceEntity? BillableResource { get; init; }

    public required int PricingModelId { get; init; }
    public PricingModelEntity PricingModel { get; init; } = null!;
    public required int FreeAllowance { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public ICollection<PlanPriceTierEntity> Tiers { get; init; } = [];
}
