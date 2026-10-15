using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Models.Approvals;
using Core.ValueObjects;
using Infrastructure.Entities;
using Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class ApprovalPolicyRepository(
    TransactionMonitoringDbContext context,
    TimeProvider timeProvider,
    IModeContext modeContext) : IApprovalPolicyRepository
{
    public async Task<ApprovalPolicy?> FindAsync(OrganizationId organizationId, CancellationToken stoppingToken)
    {
        var approvalPolicyEntity = await context.ApprovalPolicies
            .FirstOrDefaultAsync(x => x.OrganizationId == organizationId, stoppingToken);

        return approvalPolicyEntity?.ToModel();
    }

    public async Task SetAsync(SetApprovalPolicyRequest setApprovalPolicyRequest, CancellationToken stoppingToken)
    {
        var now = timeProvider.GetUtcNow();

        var minimumSeverityId = setApprovalPolicyRequest.CaseDecisionMinimumSeverity.HasValue
            ? (int)setApprovalPolicyRequest.CaseDecisionMinimumSeverity.Value
            : (int?)null;

        var matcher = setApprovalPolicyRequest.CaseDecisionMatcher?.ToBase64String();

        var approvalPolicyEntity = await context.ApprovalPolicies
            .FirstOrDefaultAsync(x => x.OrganizationId == setApprovalPolicyRequest.OrganizationId, stoppingToken);

        if (approvalPolicyEntity is null)
        {
            await context.ApprovalPolicies.AddAsync(new ApprovalPolicyEntity
            {
                ModeId = (int)modeContext.CurrentOrLive,
                OrganizationId = setApprovalPolicyRequest.OrganizationId,
                RequireDeploymentApproval = setApprovalPolicyRequest.RequireDeploymentApproval,
                CaseDecisionMinimumSeverityId = minimumSeverityId,
                CaseDecisionMatcher = matcher,
                CreatedAt = now,
                UpdatedAt = now
            }, stoppingToken);

            return;
        }

        approvalPolicyEntity.RequireDeploymentApproval = setApprovalPolicyRequest.RequireDeploymentApproval;
        approvalPolicyEntity.CaseDecisionMinimumSeverityId = minimumSeverityId;
        approvalPolicyEntity.CaseDecisionMatcher = matcher;
        approvalPolicyEntity.UpdatedAt = now;

        context.ApprovalPolicies.Update(approvalPolicyEntity);
    }
}
