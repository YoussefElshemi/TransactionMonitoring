using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record ServiceCostRateEntity : BaseEntity
{
    public required Guid ServiceCostRateId { get; init; }
    public required int BillableResourceId { get; init; }
    public BillableResourceEntity BillableResource { get; init; } = null!;
    public required decimal UnitCost { get; init; }
    public required int CurrencyId { get; init; }
    public CurrencyEntity Currency { get; init; } = null!;
    public required DateTimeOffset EffectiveFrom { get; init; }
    public required Guid? CreatedByUserId { get; init; }
    public ApplicationUserEntity? CreatedByUser { get; init; }
}
