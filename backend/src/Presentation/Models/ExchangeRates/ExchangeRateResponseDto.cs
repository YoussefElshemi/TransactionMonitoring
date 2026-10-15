namespace Presentation.Models.ExchangeRates;

public record ExchangeRateResponseDto
{
    public required Guid ExchangeRateId { get; init; }
    public required string FromCurrency { get; init; }
    public required string ToCurrency { get; init; }
    public required decimal Rate { get; init; }
    public required DateTimeOffset EffectiveFrom { get; init; }
    public required Guid? CreatedByUserId { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}
