using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.Payments;

public record RecordPaymentRequest
{
    public required OrganizationId OrganizationId { get; init; }
    public required PaymentAmount Amount { get; init; }
    public required Currency Currency { get; init; }
    public required ReceivedAt ReceivedAt { get; init; }
    public required RemittanceReference? RemittanceReference { get; init; }

    public required InvoiceId? InvoiceId { get; init; }

    public required ApplicationUserId? RecordedByUserId { get; init; }
}
