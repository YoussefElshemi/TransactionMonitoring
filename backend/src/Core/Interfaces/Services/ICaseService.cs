using Core.Enums;
using Core.Models.Approvals;
using Core.Models.Cases;
using Core.Models.Statistics;
using Core.ValueObjects;

namespace Core.Interfaces.Services;

public interface ICaseService : IPagedService<ListCaseRequest, Case>
{
    Task<Case> CreateAsync(CreateCaseRequest createCaseRequest, CancellationToken stoppingToken);
    Task<Case> GetAsync(CaseId caseId, CancellationToken stoppingToken);

    Task<EvidencePack> GetEvidencePackAsync(CaseId caseId, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(OrganizationId organizationId, CaseId caseId, IncludeDeleted includeDeleted, CancellationToken stoppingToken);
    Task AssignAsync(CaseId caseId, ApplicationUserId userId, CancellationToken stoppingToken);
    Task UnassignAsync(CaseId caseId, CancellationToken stoppingToken);
    Task<Approval?> ResolveAsync(ResolveCaseRequest resolveCaseRequest, CancellationToken stoppingToken);

    Task ApplyApprovedResolutionAsync(ResolveCaseRequest resolveCaseRequest, CancellationToken stoppingToken);
    Task ChangeStatusAsync(ChangeCaseStatusRequest changeCaseStatusRequest, CancellationToken stoppingToken);
    Task ReopenAsync(ReopenCaseRequest reopenCaseRequest, CancellationToken stoppingToken);
    Task DeleteAsync(CaseId caseId, CancellationToken stoppingToken);
    Task RecoverAsync(CaseId caseId, CancellationToken stoppingToken);
    Task<Dictionary<CaseStatus, TotalCount>> CountByStatusAsync(ListCaseRequest listCaseRequest, CancellationToken stoppingToken);
    Task<TimeSpan?> GetAverageResolutionTimeAsync(StatisticRequest statisticRequest, CancellationToken stoppingToken);
    Task<TotalCount> CountBreachedAsync(ListCaseRequest listCaseRequest, CancellationToken stoppingToken);

    Task<IReadOnlyDictionary<RuleSeverity, SlaTarget>> GetSlaTargetsAsync(OrganizationId organizationId, CancellationToken stoppingToken);
    Task SetSlaTargetsAsync(SetCaseSlaTargetsRequest setCaseSlaTargetsRequest, CancellationToken stoppingToken);
}