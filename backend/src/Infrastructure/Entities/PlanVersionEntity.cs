namespace Infrastructure.Entities;

public record PlanVersionEntity
{
    public required Guid PlanVersionId { get; init; }
    public required Guid PlanId { get; init; }
    public PlanEntity Plan { get; init; } = null!;
    public required int VersionNumber { get; init; }

    public required int CurrencyId { get; init; }
    public CurrencyEntity Currency { get; init; } = null!;

    public required decimal CostPerRequest { get; init; }

    public required decimal PlatformFee { get; init; }
    public required decimal MinimumCharge { get; init; }
    public ICollection<PlanPriceEntity> Prices { get; init; } = [];
    public required Guid? PublishedByUserId { get; init; }
    public ApplicationUserEntity? PublishedByUser { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public ICollection<SubscriptionEntity> Subscriptions { get; init; } = [];
    public ICollection<InvoiceEntity> Invoices { get; init; } = [];
}
