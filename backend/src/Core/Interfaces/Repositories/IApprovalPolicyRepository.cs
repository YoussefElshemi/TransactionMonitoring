using Core.Models.Approvals;
using Core.ValueObjects;

namespace Core.Interfaces.Repositories;

public interface IApprovalPolicyRepository
{
    Task<ApprovalPolicy?> FindAsync(OrganizationId organizationId, CancellationToken stoppingToken);

    Task SetAsync(SetApprovalPolicyRequest setApprovalPolicyRequest, CancellationToken stoppingToken);
}
