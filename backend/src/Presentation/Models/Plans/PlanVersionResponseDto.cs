namespace Presentation.Models.Plans;

public record PlanVersionResponseDto
{
    public required Guid PlanVersionId { get; init; }
    public required Guid PlanId { get; init; }
    public required int VersionNumber { get; init; }
    public required string Currency { get; init; }

    public required decimal CostPerRequest { get; init; }

    public required decimal PlatformFee { get; init; }
    public required decimal MinimumCharge { get; init; }
    public required List<PlanPriceResponseDto> Prices { get; init; }
    public required Guid? PublishedByUserId { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}
