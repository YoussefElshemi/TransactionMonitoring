using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.Payments;

public record ProviderPaymentMethod
{
    public required PaymentMethodReference PaymentMethodReference { get; init; }
    public required PaymentMethodKind PaymentMethodKind { get; init; }

    public required PaymentMethodLabel Label { get; init; }

    public required MandateReference? MandateReference { get; init; }

    public required IsDefault IsDefault { get; init; }

    public required ExpiresAt? ExpiresAt { get; init; }
}
