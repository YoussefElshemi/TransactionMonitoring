namespace Presentation.Models.Payments;

public record PaymentMethodResponseDto
{
    public required string PaymentMethodReference { get; init; }
    public required string PaymentMethodKind { get; init; }
    public required string Label { get; init; }
    public required string? MandateReference { get; init; }
    public required bool IsDefault { get; init; }
    public required DateTimeOffset? ExpiresAt { get; init; }
}
