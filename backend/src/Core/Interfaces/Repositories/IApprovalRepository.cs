using Core.Models.Approvals;
using Core.ValueObjects;

namespace Core.Interfaces.Repositories;

public interface IApprovalRepository : IPagedRepository<ListApprovalRequest, Approval>
{
    Task AddAsync(Approval approval, CancellationToken stoppingToken);
    Task<Approval> GetAsync(ApprovalId approvalId, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(OrganizationId organizationId, ApprovalId approvalId, CancellationToken stoppingToken);

    Task<Approval?> FindPendingByCaseAsync(CaseId caseId, CancellationToken stoppingToken);
    Task<Approval?> FindPendingBySuspiciousActivityReportAsync(SuspiciousActivityReportId suspiciousActivityReportId, CancellationToken stoppingToken);
    Task<Approval> ReviewAsync(ReviewApprovalRequest reviewApprovalRequest, CancellationToken stoppingToken);
}
