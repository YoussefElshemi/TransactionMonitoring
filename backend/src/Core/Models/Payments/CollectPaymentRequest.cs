using Core.ValueObjects;

namespace Core.Models.Payments;

public record CollectPaymentRequest
{
    public required InvoiceId InvoiceId { get; init; }

    public required PaymentMethodReference? PaymentMethodReference { get; init; }
}
