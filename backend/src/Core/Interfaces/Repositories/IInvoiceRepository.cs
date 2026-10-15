using Core.Models.Invoices;
using Core.Models.Revenue;
using Core.ValueObjects;

namespace Core.Interfaces.Repositories;

public interface IInvoiceRepository : IPagedRepository<ListInvoiceRequest, Invoice>
{
    Task AddAsync(Invoice invoice, CancellationToken stoppingToken);

    Task<CommitmentAmount> GetCommitmentDrawnAsync(OrganizationId organizationId, EffectiveFrom effectiveFrom, EffectiveTo effectiveTo, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(OrganizationId organizationId, InvoiceId invoiceId, IncludeDeleted includeDeleted, CancellationToken stoppingToken);
    Task<Exists> ExistsByDateAsync(OrganizationId organizationId, Month month, Year year, IncludeDeleted includeDeleted, CancellationToken stoppingToken);
    Task<Invoice> GetAsync(InvoiceId invoiceId, CancellationToken stoppingToken);

    Task<List<Invoice>> GetManyAsync(List<InvoiceId> invoiceIds, CancellationToken stoppingToken);

    Task<Invoice?> GetIssuedByDateAsync(OrganizationId organizationId, Month month, Year year, CancellationToken stoppingToken);

    Task IssueAsync(InvoiceId invoiceId, InvoiceNumber invoiceNumber, IssuedAt issuedAt, DueAt dueAt, CancellationToken stoppingToken);

    Task<List<Invoice>> GetOverdueAsync(CancellationToken stoppingToken);

    Task<List<BilledMonth>> GetBilledMonthsAsync(int firstMonthKey, int lastMonthKey, CancellationToken stoppingToken);

    Task<List<Invoice>> ListIssuedByPeriodAsync(int firstMonthKey, int lastMonthKey, CancellationToken stoppingToken);
    Task ProcessAsync(InvoiceId invoiceId, CancellationToken stoppingToken);

    Task UnprocessAsync(InvoiceId invoiceId, CancellationToken stoppingToken);
    Task DeleteAsync(InvoiceId invoiceId, CancellationToken stoppingToken);
    Task RecoverAsync(InvoiceId invoiceId, CancellationToken stoppingToken);
}