using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record ExchangeRateEntity : BaseEntity
{
    public required Guid ExchangeRateId { get; init; }
    public required int FromCurrencyId { get; init; }
    public CurrencyEntity FromCurrency { get; init; } = null!;
    public required int ToCurrencyId { get; init; }
    public CurrencyEntity ToCurrency { get; init; } = null!;
    public required decimal Rate { get; init; }
    public required DateTimeOffset EffectiveFrom { get; init; }
    public required Guid? CreatedByUserId { get; init; }
    public ApplicationUserEntity? CreatedByUser { get; init; }
}
