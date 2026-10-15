using Core.Enums;
using Core.Models.CaseReasonCodes;
using Core.Models.Common;
using Core.ValueObjects;

namespace Core.Interfaces.Services;

public interface ICaseReasonCodeService
{
    Task<CaseReasonCode> CreateAsync(CreateCaseReasonCodeRequest createCaseReasonCodeRequest, CancellationToken stoppingToken);
    Task<CaseReasonCode> GetAsync(CaseReasonCodeId caseReasonCodeId, IncludeDeleted includeDeleted, CancellationToken stoppingToken);
    Task<CaseReasonCode?> FindByCodeAsync(OrganizationId organizationId, CaseReasonCodeName code, CancellationToken stoppingToken);
    Task<PagedResponse<CaseReasonCode>> ListAsync(ListCaseReasonCodeRequest listCaseReasonCodeRequest, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(OrganizationId organizationId, CaseReasonCodeId caseReasonCodeId, IncludeDeleted includeDeleted, CancellationToken stoppingToken);
    Task<Exists> AnyEnabledAsync(OrganizationId organizationId, CaseDisposition? disposition, CancellationToken stoppingToken);
    Task<CaseReasonCodeUsage> GetUsageAsync(CaseReasonCodeId caseReasonCodeId, CancellationToken stoppingToken);
    Task UpdateAsync(UpdateCaseReasonCodeRequest updateCaseReasonCodeRequest, CancellationToken stoppingToken);
    Task DeleteAsync(CaseReasonCodeId caseReasonCodeId, CancellationToken stoppingToken);
    Task RecoverAsync(CaseReasonCodeId caseReasonCodeId, CancellationToken stoppingToken);
}
