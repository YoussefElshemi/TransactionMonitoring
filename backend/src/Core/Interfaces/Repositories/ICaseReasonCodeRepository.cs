using Core.Enums;
using Core.Models.CaseReasonCodes;
using Core.ValueObjects;

namespace Core.Interfaces.Repositories;

public interface ICaseReasonCodeRepository : IPagedRepository<ListCaseReasonCodeRequest, CaseReasonCode>
{
    Task AddAsync(CaseReasonCode caseReasonCode, CancellationToken stoppingToken);
    Task<CaseReasonCode> GetAsync(CaseReasonCodeId caseReasonCodeId, IncludeDeleted includeDeleted, CancellationToken stoppingToken);
    Task<CaseReasonCode?> FindByCodeAsync(OrganizationId organizationId, CaseReasonCodeName code, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(OrganizationId organizationId, CaseReasonCodeId caseReasonCodeId, IncludeDeleted includeDeleted, CancellationToken stoppingToken);
    Task<Exists> AnyEnabledAsync(OrganizationId organizationId, CaseDisposition? disposition, CancellationToken stoppingToken);
    Task<CaseReasonCodeUsage> GetUsageAsync(CaseReasonCodeId caseReasonCodeId, CancellationToken stoppingToken);
    Task UpdateAsync(UpdateCaseReasonCodeRequest updateCaseReasonCodeRequest, CancellationToken stoppingToken);
    Task DeleteAsync(CaseReasonCodeId caseReasonCodeId, CancellationToken stoppingToken);
    Task RecoverAsync(CaseReasonCodeId caseReasonCodeId, CancellationToken stoppingToken);
}
