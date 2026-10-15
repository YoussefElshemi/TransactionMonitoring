using Core.Enums;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Mappers;
using Core.Models.CaseReasonCodes;
using Core.Models.Common;
using Core.ValueObjects;

namespace Core.Services;

public class CaseReasonCodeService(
    ICaseReasonCodeRepository caseReasonCodeRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IModeContext modeContext) : ICaseReasonCodeService
{
    public async Task<CaseReasonCode> CreateAsync(CreateCaseReasonCodeRequest createCaseReasonCodeRequest, CancellationToken stoppingToken)
    {
        var caseReasonCode = createCaseReasonCodeRequest.ToModel(timeProvider, modeContext.CurrentOrLive);

        await caseReasonCodeRepository.AddAsync(caseReasonCode, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);

        return caseReasonCode;
    }

    public Task<CaseReasonCode> GetAsync(CaseReasonCodeId caseReasonCodeId, IncludeDeleted includeDeleted, CancellationToken stoppingToken)
    {
        return caseReasonCodeRepository.GetAsync(caseReasonCodeId, includeDeleted, stoppingToken);
    }

    public Task<CaseReasonCode?> FindByCodeAsync(OrganizationId organizationId, CaseReasonCodeName code, CancellationToken stoppingToken)
    {
        return caseReasonCodeRepository.FindByCodeAsync(organizationId, code, stoppingToken);
    }

    public Task<PagedResponse<CaseReasonCode>> ListAsync(ListCaseReasonCodeRequest listCaseReasonCodeRequest, CancellationToken stoppingToken)
    {
        return caseReasonCodeRepository.ListAsync(listCaseReasonCodeRequest, stoppingToken);
    }

    public Task<Exists> ExistsAsync(OrganizationId organizationId, CaseReasonCodeId caseReasonCodeId, IncludeDeleted includeDeleted, CancellationToken stoppingToken)
    {
        return caseReasonCodeRepository.ExistsAsync(organizationId, caseReasonCodeId, includeDeleted, stoppingToken);
    }

    public Task<Exists> AnyEnabledAsync(OrganizationId organizationId, CaseDisposition? disposition, CancellationToken stoppingToken)
    {
        return caseReasonCodeRepository.AnyEnabledAsync(organizationId, disposition, stoppingToken);
    }

    public Task<CaseReasonCodeUsage> GetUsageAsync(CaseReasonCodeId caseReasonCodeId, CancellationToken stoppingToken)
    {
        return caseReasonCodeRepository.GetUsageAsync(caseReasonCodeId, stoppingToken);
    }

    public async Task UpdateAsync(UpdateCaseReasonCodeRequest updateCaseReasonCodeRequest, CancellationToken stoppingToken)
    {
        await caseReasonCodeRepository.UpdateAsync(updateCaseReasonCodeRequest, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);
    }

    public async Task DeleteAsync(CaseReasonCodeId caseReasonCodeId, CancellationToken stoppingToken)
    {
        await caseReasonCodeRepository.DeleteAsync(caseReasonCodeId, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);
    }

    public async Task RecoverAsync(CaseReasonCodeId caseReasonCodeId, CancellationToken stoppingToken)
    {
        await caseReasonCodeRepository.RecoverAsync(caseReasonCodeId, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);
    }
}
