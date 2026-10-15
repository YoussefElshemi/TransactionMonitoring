using Core.Models.CaseNotes;
using Core.ValueObjects;

namespace Core.Interfaces.Repositories;

public interface ICaseNoteRepository : IPagedRepository<ListCaseNoteRequest, CaseNote>
{
    Task AddAsync(CaseNote caseNote, CancellationToken stoppingToken);
    Task UpdateAsync(UpdateCaseNoteRequest updateCaseNoteRequest, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(OrganizationId organizationId, CaseId caseId, CaseNoteId caseNoteId, IncludeDeleted includeDeleted, CancellationToken stoppingToken);
    Task<CaseNote> GetAsync(CaseNoteId caseNoteId, CancellationToken stoppingToken);
    Task DeleteAsync(CaseNoteId caseNoteId, CancellationToken stoppingToken);
    Task RecoverAsync(CaseNoteId caseNoteId, CancellationToken stoppingToken);
}