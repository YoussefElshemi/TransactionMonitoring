using Core.Models.Adjustments;
using Core.ValueObjects;

namespace Core.Interfaces.Repositories;

public interface IInvoiceAdjustmentRepository : IPagedRepository<ListInvoiceAdjustmentRequest, InvoiceAdjustment>
{
    Task AddAsync(InvoiceAdjustment invoiceAdjustment, CancellationToken stoppingToken);
    Task<InvoiceAdjustment> GetAsync(InvoiceAdjustmentId invoiceAdjustmentId, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(OrganizationId organizationId, InvoiceAdjustmentId invoiceAdjustmentId, CancellationToken stoppingToken);

    Task<List<InvoiceAdjustment>> GetUnappliedAsync(OrganizationId organizationId, Month month, Year year, CancellationToken stoppingToken);

    Task ApplyAsync(InvoiceAdjustmentId invoiceAdjustmentId, InvoiceId invoiceId, CancellationToken stoppingToken);

    Task ReleaseAsync(InvoiceId invoiceId, CancellationToken stoppingToken);

    Task ReapplyAsync(InvoiceId invoiceId, CancellationToken stoppingToken);

    Task DeleteAsync(InvoiceAdjustmentId invoiceAdjustmentId, CancellationToken stoppingToken);
}
