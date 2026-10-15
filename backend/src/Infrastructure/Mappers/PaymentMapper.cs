using Core.Enums;
using Core.Models.Payments;
using Core.ValueObjects;
using Infrastructure.Entities;

namespace Infrastructure.Mappers;

public static class PaymentMapper
{
    public static Payment ToModel(this PaymentEntity paymentEntity)
    {
        return new Payment
        {
            PaymentId = new PaymentId(paymentEntity.PaymentId),
            OrganizationId = new OrganizationId(paymentEntity.OrganizationId),
            Organization = paymentEntity.Organization?.ToModel(),
            Amount = new PaymentAmount(paymentEntity.Amount),
            Currency = (Currency)paymentEntity.CurrencyId,
            PaymentStatus = (PaymentStatus)paymentEntity.PaymentStatusId,
            PaymentMethodKind = (PaymentMethodKind)paymentEntity.PaymentMethodKindId,
            ProviderPaymentReference = paymentEntity.ProviderPaymentReference is null
                ? null
                : new ProviderPaymentReference(paymentEntity.ProviderPaymentReference),
            PaymentMethodReference = paymentEntity.PaymentMethodReference is null
                ? null
                : new PaymentMethodReference(paymentEntity.PaymentMethodReference),
            PaymentMethodLabel = paymentEntity.PaymentMethodLabel is null
                ? null
                : new PaymentMethodLabel(paymentEntity.PaymentMethodLabel),
            RemittanceReference = paymentEntity.RemittanceReference is null
                ? null
                : new RemittanceReference(paymentEntity.RemittanceReference),
            ReceiptNumber = paymentEntity.ReceiptNumber is null
                ? null
                : new ReceiptNumber(paymentEntity.ReceiptNumber),
            RaisedForInvoiceId = paymentEntity.RaisedForInvoiceId.HasValue
                ? new InvoiceId(paymentEntity.RaisedForInvoiceId.Value)
                : null,
            ReceivedAt = new ReceivedAt(paymentEntity.ReceivedAt),
            SettledAt = paymentEntity.SettledAt.HasValue ? new SettledAt(paymentEntity.SettledAt.Value) : null,
            ReturnedAt = paymentEntity.ReturnedAt.HasValue ? new ReturnedAt(paymentEntity.ReturnedAt.Value) : null,
            FailureCode = paymentEntity.FailureCode is null ? null : new FailureCode(paymentEntity.FailureCode),
            FailureReason = paymentEntity.FailureReason is null ? null : new FailureReason(paymentEntity.FailureReason),
            RecordedByUserId = paymentEntity.RecordedByUserId.HasValue
                ? new ApplicationUserId(paymentEntity.RecordedByUserId.Value)
                : null,
            IsDeleted = new IsDeleted(paymentEntity.IsDeleted),
            DeletedAt = paymentEntity.DeletedAt.HasValue ? new DeletedAt(paymentEntity.DeletedAt.Value) : null,
            CreatedAt = new CreatedAt(paymentEntity.CreatedAt),
            UpdatedAt = new UpdatedAt(paymentEntity.UpdatedAt)
        };
    }

    public static PaymentEntity ToEntity(this Payment payment)
    {
        return new PaymentEntity
        {
            PaymentId = payment.PaymentId,
            OrganizationId = payment.OrganizationId,
            Amount = payment.Amount,
            CurrencyId = (int)payment.Currency,
            PaymentStatusId = (int)payment.PaymentStatus,
            PaymentMethodKindId = (int)payment.PaymentMethodKind,
            ProviderPaymentReference = payment.ProviderPaymentReference?.ToString(),
            PaymentMethodReference = payment.PaymentMethodReference?.ToString(),
            PaymentMethodLabel = payment.PaymentMethodLabel?.ToString(),
            RemittanceReference = payment.RemittanceReference?.ToString(),
            ReceiptNumber = payment.ReceiptNumber?.ToString(),
            RaisedForInvoiceId = payment.RaisedForInvoiceId.HasValue ? payment.RaisedForInvoiceId.Value : null,
            ReceivedAt = payment.ReceivedAt,
            SettledAt = payment.SettledAt.HasValue ? payment.SettledAt.Value : null,
            ReturnedAt = payment.ReturnedAt.HasValue ? payment.ReturnedAt.Value : null,
            FailureCode = payment.FailureCode?.ToString(),
            FailureReason = payment.FailureReason?.ToString(),
            RecordedByUserId = payment.RecordedByUserId.HasValue ? payment.RecordedByUserId.Value : null,
            IsDeleted = payment.IsDeleted,
            DeletedAt = payment.DeletedAt.HasValue ? payment.DeletedAt.Value : null,
            CreatedAt = payment.CreatedAt,
            UpdatedAt = payment.UpdatedAt
        };
    }
}
