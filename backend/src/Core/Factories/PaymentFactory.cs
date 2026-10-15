using Core.Enums;
using Core.Models.Invoices;
using Core.Models.Payments;
using Core.Models.Providers;
using Core.ValueObjects;

namespace Core.Factories;

public static class PaymentFactory
{
    public static Payment FromCharge(
        Invoice invoice,
        PaymentAmount amount,
        ProviderChargeResult providerChargeResult,
        TimeProvider timeProvider)
    {
        var now = timeProvider.GetUtcNow();

        return new Payment
        {
            PaymentId = new PaymentId(Guid.NewGuid()),
            OrganizationId = invoice.OrganizationId,
            Organization = null,
            Amount = amount,
            Currency = invoice.Currency,
            PaymentStatus = providerChargeResult.PaymentStatus,

            PaymentMethodKind = providerChargeResult.PaymentMethodKind ?? PaymentMethodKind.Card,
            ProviderPaymentReference = providerChargeResult.PaymentReference,
            PaymentMethodReference = providerChargeResult.PaymentMethodReference,
            PaymentMethodLabel = providerChargeResult.PaymentMethodLabel,
            RemittanceReference = null,
            ReceiptNumber = null,
            RaisedForInvoiceId = invoice.InvoiceId,
            ReceivedAt = new ReceivedAt(now),

            SettledAt = providerChargeResult.PaymentStatus is PaymentStatus.Settled ? new SettledAt(now) : null,
            ReturnedAt = null,
            FailureCode = providerChargeResult.FailureCode,
            FailureReason = providerChargeResult.FailureReason,
            RecordedByUserId = null,
            IsDeleted = new IsDeleted(false),
            DeletedAt = null,
            CreatedAt = new CreatedAt(now),
            UpdatedAt = new UpdatedAt(now)
        };
    }

    public static Payment FromRecord(
        RecordPaymentRequest recordPaymentRequest,
        ReceiptNumber receiptNumber,
        TimeProvider timeProvider)
    {
        var now = timeProvider.GetUtcNow();

        return new Payment
        {
            PaymentId = new PaymentId(Guid.NewGuid()),
            OrganizationId = recordPaymentRequest.OrganizationId,
            Organization = null,
            Amount = recordPaymentRequest.Amount,
            Currency = recordPaymentRequest.Currency,
            PaymentStatus = PaymentStatus.Settled,
            PaymentMethodKind = PaymentMethodKind.BankTransfer,
            ProviderPaymentReference = null,
            PaymentMethodReference = null,
            PaymentMethodLabel = null,
            RemittanceReference = recordPaymentRequest.RemittanceReference,
            ReceiptNumber = receiptNumber,
            RaisedForInvoiceId = recordPaymentRequest.InvoiceId,

            ReceivedAt = recordPaymentRequest.ReceivedAt,
            SettledAt = new SettledAt(recordPaymentRequest.ReceivedAt),
            ReturnedAt = null,
            FailureCode = null,
            FailureReason = null,
            RecordedByUserId = recordPaymentRequest.RecordedByUserId,
            IsDeleted = new IsDeleted(false),
            DeletedAt = null,
            CreatedAt = new CreatedAt(now),
            UpdatedAt = new UpdatedAt(now)
        };
    }
}
