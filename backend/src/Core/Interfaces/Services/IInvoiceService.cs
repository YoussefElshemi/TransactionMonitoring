using Core.Models.Invoices;
using Core.ValueObjects;

namespace Core.Interfaces.Services;

public interface IInvoiceService : IPagedService<ListInvoiceRequest, Invoice>
{
    Task<Invoice> GenerateAsync(GenerateInvoiceRequest generateInvoiceRequest, CancellationToken stoppingToken);
    Task<Invoice> IssueAsync(InvoiceId invoiceId, CancellationToken stoppingToken);
    Task<ExportedInvoice> ExportAsync(InvoiceId invoiceId, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(OrganizationId organizationId, InvoiceId invoiceId, IncludeDeleted includeDeleted, CancellationToken stoppingToken);
    Task<Exists> ExistsByDateAsync(OrganizationId organizationId, Month month, Year year, IncludeDeleted includeDeleted, CancellationToken stoppingToken);
    Task<Invoice> GetAsync(InvoiceId invoiceId, CancellationToken stoppingToken);
    Task<Invoice?> GetIssuedByDateAsync(OrganizationId organizationId, Month month, Year year, CancellationToken stoppingToken);
    Task<List<Invoice>> GetOverdueAsync(CancellationToken stoppingToken);
    Task ProcessAsync(InvoiceId invoiceId, CancellationToken stoppingToken);
    Task DeleteAsync(InvoiceId invoiceId, CancellationToken stoppingToken);
    Task RecoverAsync(InvoiceId invoiceId, CancellationToken stoppingToken);
}