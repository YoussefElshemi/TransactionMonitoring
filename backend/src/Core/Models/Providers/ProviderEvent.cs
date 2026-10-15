using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.Providers;

public record ProviderEvent
{
    public required ProviderEventReference EventReference { get; init; }
    public required ProviderEventType EventType { get; init; }

    public required Payload Payload { get; init; }

    public required ProviderPaymentReference? PaymentReference { get; init; }

    public required PaymentStatus? PaymentStatus { get; init; }

    public required PaymentMethodReference? PaymentMethodReference { get; init; }
    public required PaymentMethodLabel? PaymentMethodLabel { get; init; }
    public required PaymentMethodKind? PaymentMethodKind { get; init; }

    public required FailureCode? FailureCode { get; init; }
    public required FailureReason? FailureReason { get; init; }
}
