using Core.Enums;
using Core.Models.Payments;
using Core.ValueObjects;

namespace Core.Interfaces.Repositories;

public interface IPaymentRepository : IPagedRepository<ListPaymentRequest, Payment>
{
    Task AddAsync(Payment payment, CancellationToken stoppingToken);
    Task<Payment> GetAsync(PaymentId paymentId, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(OrganizationId organizationId, PaymentId paymentId, IncludeDeleted includeDeleted, CancellationToken stoppingToken);

    Task<Attempts> GetAttemptCountAsync(InvoiceId invoiceId, CancellationToken stoppingToken);

    Task<Payment?> FindByProviderReferenceAsync(ProviderPaymentReference providerPaymentReference, CancellationToken stoppingToken);

    Task SettleAsync(PaymentId paymentId, SettledAt settledAt, ReceiptNumber receiptNumber, CancellationToken stoppingToken);
    Task FailAsync(PaymentId paymentId, FailureCode? failureCode, FailureReason failureReason, CancellationToken stoppingToken);
    Task ReturnAsync(PaymentId paymentId, ReturnedAt returnedAt, FailureCode? failureCode, FailureReason failureReason, CancellationToken stoppingToken);

    Task RecordMethodAsync(
        PaymentId paymentId,
        PaymentMethodKind paymentMethodKind,
        PaymentMethodReference? paymentMethodReference,
        PaymentMethodLabel? paymentMethodLabel,
        CancellationToken stoppingToken);
}
