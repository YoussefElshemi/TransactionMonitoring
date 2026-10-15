using Core.Enums;
using Core.Models.BaseModels;
using Core.Models.Organizations;
using Core.ValueObjects;

namespace Core.Models.Payments;

public record Payment : DeletableModel
{
    public required PaymentId PaymentId { get; init; }
    public required OrganizationId OrganizationId { get; init; }
    public required Organization? Organization { get; init; }

    public required PaymentAmount Amount { get; init; }

    public required Currency Currency { get; init; }

    public required PaymentStatus PaymentStatus { get; init; }
    public required PaymentMethodKind PaymentMethodKind { get; init; }

    public required ProviderPaymentReference? ProviderPaymentReference { get; init; }
    public required PaymentMethodReference? PaymentMethodReference { get; init; }
    public required PaymentMethodLabel? PaymentMethodLabel { get; init; }

    public required RemittanceReference? RemittanceReference { get; init; }

    public required ReceiptNumber? ReceiptNumber { get; init; }

    public required InvoiceId? RaisedForInvoiceId { get; init; }

    public required ReceivedAt ReceivedAt { get; init; }
    public required SettledAt? SettledAt { get; init; }
    public required ReturnedAt? ReturnedAt { get; init; }

    public required FailureCode? FailureCode { get; init; }
    public required FailureReason? FailureReason { get; init; }

    public required ApplicationUserId? RecordedByUserId { get; init; }
}
