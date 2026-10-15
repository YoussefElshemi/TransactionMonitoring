using Core.Models.CreditNotes;
using Core.ValueObjects;

namespace Core.Interfaces.Services;

public interface ICreditNoteService : IPagedService<ListCreditNoteRequest, CreditNote>
{
    Task<CreditNote> CreateAsync(CreateCreditNoteRequest createCreditNoteRequest, CancellationToken stoppingToken);
    Task<CreditNote> GetAsync(CreditNoteId creditNoteId, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(OrganizationId organizationId, CreditNoteId creditNoteId, CancellationToken stoppingToken);
    Task<ExportedCreditNote> ExportAsync(CreditNoteId creditNoteId, CancellationToken stoppingToken);
}
