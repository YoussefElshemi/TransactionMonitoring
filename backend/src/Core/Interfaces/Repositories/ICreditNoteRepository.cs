using Core.Models.CreditNotes;
using Core.ValueObjects;

namespace Core.Interfaces.Repositories;

public interface ICreditNoteRepository : IPagedRepository<ListCreditNoteRequest, CreditNote>
{
    Task AddAsync(CreditNote creditNote, CancellationToken stoppingToken);
    Task<CreditNote> GetAsync(CreditNoteId creditNoteId, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(OrganizationId organizationId, CreditNoteId creditNoteId, CancellationToken stoppingToken);

    Task<NetAmount> GetCreditedNetAsync(InvoiceId invoiceId, CancellationToken stoppingToken);

    Task<InvoiceAmount> GetCreditedAmountAsync(InvoiceId invoiceId, CancellationToken stoppingToken);

    Task<Dictionary<Guid, InvoiceAmount>> GetCreditedAmountManyAsync(List<InvoiceId> invoiceIds, CancellationToken stoppingToken);

    Task<List<CreditNote>> ListByInvoicePeriodAsync(int firstMonthKey, int lastMonthKey, CancellationToken stoppingToken);
}
