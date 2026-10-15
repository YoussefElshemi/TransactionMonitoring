using Core.Enums;
using Core.Models.Payments;
using Core.ValueObjects;
using Presentation.Models.Payments;

namespace Presentation.Mappers.Payments;

public static class PaymentDtoMapper
{
    public static ListPaymentRequest ToModel(this ListPaymentRequestDto dto)
    {
        return new ListPaymentRequest
        {
            OrganizationId = new OrganizationId(dto.OrganizationId),

            PaymentStatus = Enum.TryParse<PaymentStatus>(dto.PaymentStatus, ignoreCase: true, out var paymentStatus)
                ? paymentStatus
                : null,
            PageNumber = new PageNumber(dto.PageNumber),
            PageSize = new PageSize(dto.PageSize),
            FromDate = new DateRange(dto.FromDate),
            ToDate = new DateRange(dto.ToDate)
        };
    }

    public static RecordPaymentRequest ToModel(this RecordPaymentRequestDto dto)
    {
        return new RecordPaymentRequest
        {
            OrganizationId = new OrganizationId(dto.OrganizationId),
            Amount = new PaymentAmount(dto.Body.Amount),
            Currency = Enum.Parse<Currency>(dto.Body.Currency, ignoreCase: true),
            ReceivedAt = new ReceivedAt(dto.Body.ReceivedAt),
            RemittanceReference = string.IsNullOrWhiteSpace(dto.Body.RemittanceReference)
                ? null
                : new RemittanceReference(dto.Body.RemittanceReference),
            InvoiceId = dto.Body.InvoiceId.HasValue ? new InvoiceId(dto.Body.InvoiceId.Value) : null,
            RecordedByUserId = dto.RecordedByUserId.HasValue ? new ApplicationUserId(dto.RecordedByUserId.Value) : null
        };
    }

    public static CollectPaymentRequest ToModel(this CollectPaymentRequestDto dto)
    {
        return new CollectPaymentRequest
        {
            InvoiceId = new InvoiceId(dto.InvoiceId),
            PaymentMethodReference = string.IsNullOrWhiteSpace(dto.Body?.PaymentMethodReference)
                ? null
                : new PaymentMethodReference(dto.Body.PaymentMethodReference)
        };
    }

    public static PaymentResponseDto ToDto(this Payment payment)
    {
        return new PaymentResponseDto
        {
            PaymentId = payment.PaymentId,
            OrganizationId = payment.OrganizationId,
            Amount = payment.Amount,
            Currency = payment.Currency.ToString(),
            PaymentStatus = payment.PaymentStatus.ToString(),
            PaymentMethodKind = payment.PaymentMethodKind.ToString(),
            PaymentMethodLabel = payment.PaymentMethodLabel?.ToString(),
            RemittanceReference = payment.RemittanceReference?.ToString(),
            ReceiptNumber = payment.ReceiptNumber?.ToString(),
            RaisedForInvoiceId = payment.RaisedForInvoiceId.HasValue ? payment.RaisedForInvoiceId.Value : null,
            ReceivedAt = payment.ReceivedAt,
            SettledAt = payment.SettledAt.HasValue ? payment.SettledAt.Value : null,
            ReturnedAt = payment.ReturnedAt.HasValue ? payment.ReturnedAt.Value : null,

            FailureReason = payment.FailureReason?.ToString(),
            RecordedByUserId = payment.RecordedByUserId.HasValue ? payment.RecordedByUserId.Value : null,
            CreatedAt = payment.CreatedAt,
            UpdatedAt = payment.UpdatedAt
        };
    }

    public static PaymentMethodResponseDto ToDto(this ProviderPaymentMethod providerPaymentMethod)
    {
        return new PaymentMethodResponseDto
        {
            PaymentMethodReference = providerPaymentMethod.PaymentMethodReference,
            PaymentMethodKind = providerPaymentMethod.PaymentMethodKind.ToString(),
            Label = providerPaymentMethod.Label,
            MandateReference = providerPaymentMethod.MandateReference?.ToString(),
            IsDefault = providerPaymentMethod.IsDefault,
            ExpiresAt = providerPaymentMethod.ExpiresAt.HasValue ? providerPaymentMethod.ExpiresAt.Value : null
        };
    }

    public static InvoiceSettlementResponseDto ToDto(this InvoiceSettlement invoiceSettlement)
    {
        return new InvoiceSettlementResponseDto
        {
            InvoiceId = invoiceSettlement.InvoiceId,
            Amount = invoiceSettlement.Amount,
            AllocatedAmount = invoiceSettlement.AllocatedAmount,
            CreditedAmount = invoiceSettlement.CreditedAmount,
            OutstandingAmount = invoiceSettlement.OutstandingAmount,
            IsSettled = invoiceSettlement.IsSettled
        };
    }
}
