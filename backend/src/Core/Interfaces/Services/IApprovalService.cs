using Core.Models.Approvals;
using Core.ValueObjects;

namespace Core.Interfaces.Services;

public interface IApprovalService : IPagedService<ListApprovalRequest, Approval>
{
    Task<Approval> GetAsync(ApprovalId approvalId, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(OrganizationId organizationId, ApprovalId approvalId, CancellationToken stoppingToken);

    Task<Approval> ReviewAsync(ReviewApprovalRequest reviewApprovalRequest, CancellationToken stoppingToken);

    Task<ApprovalPolicy?> FindPolicyAsync(OrganizationId organizationId, CancellationToken stoppingToken);
    Task SetPolicyAsync(SetApprovalPolicyRequest setApprovalPolicyRequest, CancellationToken stoppingToken);
}
