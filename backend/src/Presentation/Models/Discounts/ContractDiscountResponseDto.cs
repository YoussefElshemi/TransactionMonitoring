namespace Presentation.Models.Discounts;

public record ContractDiscountResponseDto
{
    public required Guid ContractDiscountId { get; init; }
    public required Guid OrganizationId { get; init; }
    public required string DiscountType { get; init; }
    public required decimal Value { get; init; }
    public required string Description { get; init; }
    public required DateTimeOffset EffectiveFrom { get; init; }
    public required DateTimeOffset? EffectiveTo { get; init; }
    public required Guid? CreatedByUserId { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
}
