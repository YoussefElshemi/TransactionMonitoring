using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Mappers;
using Core.Models.CaseNotes;
using Core.Models.Common;
using Core.ValueObjects;

namespace Core.Services;

public class CaseNoteService(
    ICaseNoteRepository caseNoteRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IModeContext modeContext) : ICaseNoteService
{
    public async Task<CaseNote> CreateAsync(CreateCaseNoteRequest createCaseNoteRequest, CancellationToken stoppingToken)
    {
        var caseNote = createCaseNoteRequest.ToModel(timeProvider, modeContext.CurrentOrLive);

        await caseNoteRepository.AddAsync(caseNote, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);

        return await GetAsync(caseNote.CaseNoteId, stoppingToken);
    }

    public async Task UpdateAsync(UpdateCaseNoteRequest updateCaseNoteRequest, CancellationToken stoppingToken)
    {
        await caseNoteRepository.UpdateAsync(updateCaseNoteRequest, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);
    }

    public Task<Exists> ExistsAsync(OrganizationId organizationId, CaseId caseId, CaseNoteId caseNoteId, IncludeDeleted includeDeleted, CancellationToken stoppingToken)
    {
        return caseNoteRepository.ExistsAsync(organizationId, caseId, caseNoteId, includeDeleted, stoppingToken);
    }

    public Task<PagedResponse<CaseNote>> ListAsync(ListCaseNoteRequest listCaseNoteRequest, CancellationToken stoppingToken)
    {
        return caseNoteRepository.ListAsync(listCaseNoteRequest, stoppingToken);
    }

    public Task<CaseNote> GetAsync(CaseNoteId caseNoteId, CancellationToken stoppingToken)
    {
        return caseNoteRepository.GetAsync(caseNoteId, stoppingToken);
    }

    public async Task DeleteAsync(CaseNoteId caseNoteId, CancellationToken stoppingToken)
    {
        await caseNoteRepository.DeleteAsync(caseNoteId, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);
    }

    public async Task RecoverAsync(CaseNoteId caseNoteId, CancellationToken stoppingToken)
    {
        await caseNoteRepository.RecoverAsync(caseNoteId, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);
    }

    public Task<TotalCount> GetCountAsync(ListCaseNoteRequest listCaseNoteRequest, CancellationToken stoppingToken)
    {
        return caseNoteRepository.GetCountAsync(listCaseNoteRequest, stoppingToken);
    }
}