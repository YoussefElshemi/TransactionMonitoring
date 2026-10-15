using Core.Models.Approvals;
using Core.Models.Common;
using Core.Models.SuspiciousActivityReports;
using Core.ValueObjects;

namespace Core.Interfaces.Services;

public interface ISuspiciousActivityReportService
{
    Task<SuspiciousActivityReport> OpenAsync(OpenSuspiciousActivityReportRequest openSuspiciousActivityReportRequest, CancellationToken stoppingToken);
    Task<SuspiciousActivityReport> GetAsync(SuspiciousActivityReportId suspiciousActivityReportId, CancellationToken stoppingToken);
    Task<List<SuspiciousActivityReport>> ListByIdsAsync(IReadOnlyCollection<SuspiciousActivityReportId> suspiciousActivityReportIds, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(OrganizationId organizationId, SuspiciousActivityReportId suspiciousActivityReportId, CancellationToken stoppingToken);
    Task<PagedResponse<SuspiciousActivityReport>> ListAsync(ListSuspiciousActivityReportRequest listSuspiciousActivityReportRequest, CancellationToken stoppingToken);
    Task<TotalCount> GetCountAsync(ListSuspiciousActivityReportRequest listSuspiciousActivityReportRequest, CancellationToken stoppingToken);
    Task<SuspiciousActivityReport?> FindByCaseAsync(CaseId caseId, CancellationToken stoppingToken);
    Task<SuspiciousActivityReport> UpdateNarrativeAsync(UpdateSuspiciousActivityReportRequest updateSuspiciousActivityReportRequest, CancellationToken stoppingToken);

    Task<SuspiciousActivityReport> RecomposeNarrativeAsync(SuspiciousActivityReportId suspiciousActivityReportId, CancellationToken stoppingToken);

    Task<Approval?> SubmitAsync(SubmitSuspiciousActivityReportRequest submitSuspiciousActivityReportRequest, CancellationToken stoppingToken);

    Task ApplyApprovedSubmissionAsync(SuspiciousActivityReportId suspiciousActivityReportId, CancellationToken stoppingToken);
    Task ApplyRejectedSubmissionAsync(SuspiciousActivityReportId suspiciousActivityReportId, CancellationToken stoppingToken);
    Task<SuspiciousActivityReport> FileAsync(FileSuspiciousActivityReportRequest fileSuspiciousActivityReportRequest, CancellationToken stoppingToken);
    Task<SuspiciousActivityReport> AcknowledgeAsync(AcknowledgeSuspiciousActivityReportRequest acknowledgeSuspiciousActivityReportRequest, CancellationToken stoppingToken);
    Task<SuspiciousActivityReport> DeclineAsync(DeclineSuspiciousActivityReportRequest declineSuspiciousActivityReportRequest, CancellationToken stoppingToken);
    Task<SuspiciousActivityReportExport> ExportAsync(SuspiciousActivityReportId suspiciousActivityReportId, CancellationToken stoppingToken);
    Task<SuspiciousActivityReportPolicy?> FindPolicyAsync(OrganizationId organizationId, CancellationToken stoppingToken);
    Task SetPolicyAsync(SetSuspiciousActivityReportPolicyRequest setSuspiciousActivityReportPolicyRequest, CancellationToken stoppingToken);
}
