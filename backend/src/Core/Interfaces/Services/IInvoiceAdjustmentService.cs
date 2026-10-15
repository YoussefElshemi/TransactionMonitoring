using Core.Models.Adjustments;
using Core.ValueObjects;

namespace Core.Interfaces.Services;

public interface IInvoiceAdjustmentService : IPagedService<ListInvoiceAdjustmentRequest, InvoiceAdjustment>
{
    Task<InvoiceAdjustment> CreateAsync(CreateInvoiceAdjustmentRequest createInvoiceAdjustmentRequest, CancellationToken stoppingToken);
    Task<InvoiceAdjustment> GetAsync(InvoiceAdjustmentId invoiceAdjustmentId, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(OrganizationId organizationId, InvoiceAdjustmentId invoiceAdjustmentId, CancellationToken stoppingToken);
    Task<List<InvoiceAdjustment>> GetUnappliedAsync(OrganizationId organizationId, Month month, Year year, CancellationToken stoppingToken);
    Task DeleteAsync(InvoiceAdjustmentId invoiceAdjustmentId, CancellationToken stoppingToken);
}
