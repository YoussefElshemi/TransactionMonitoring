using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.Providers;

public record ProviderChargeRequest
{
    public required ProviderCustomerReference CustomerReference { get; init; }

    public required PaymentMethodReference? PaymentMethodReference { get; init; }

    public required PaymentAmount Amount { get; init; }
    public required Currency Currency { get; init; }

    public required InvoiceNumber InvoiceNumber { get; init; }

    // Derived from the invoice and attempt so a retried job cannot charge twice.
    public required ProviderIdempotencyKey IdempotencyKey { get; init; }
}
