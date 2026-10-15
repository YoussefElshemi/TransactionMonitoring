using Core.Models.Payments;
using Core.Models.Providers;
using Core.ValueObjects;

namespace Core.Interfaces.Services;

public interface IPaymentService : IPagedService<ListPaymentRequest, Payment>
{
    Task<Payment> CollectAsync(CollectPaymentRequest collectPaymentRequest, CancellationToken stoppingToken);

    Task<Payment> RecordAsync(RecordPaymentRequest recordPaymentRequest, CancellationToken stoppingToken);

    Task<PaymentId?> HandleEventAsync(ProviderEvent providerEvent, CancellationToken stoppingToken);

    Task<Payment> GetAsync(PaymentId paymentId, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(OrganizationId organizationId, PaymentId paymentId, IncludeDeleted includeDeleted, CancellationToken stoppingToken);

    Task<ExportedReceipt> ExportReceiptAsync(PaymentId paymentId, CancellationToken stoppingToken);

    Task<InvoiceSettlement> GetSettlementAsync(InvoiceId invoiceId, CancellationToken stoppingToken);
    Task<List<InvoiceSettlement>> GetSettlementsAsync(List<InvoiceId> invoiceIds, CancellationToken stoppingToken);
}
