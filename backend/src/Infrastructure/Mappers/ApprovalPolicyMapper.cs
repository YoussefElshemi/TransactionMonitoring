using Core.Enums;
using Core.Models.Approvals;
using Core.ValueObjects;
using Infrastructure.Entities;

namespace Infrastructure.Mappers;

public static class ApprovalPolicyMapper
{
    public static ApprovalPolicy ToModel(this ApprovalPolicyEntity approvalPolicyEntity)
    {
        return new ApprovalPolicy
        {
            Mode = (Mode)approvalPolicyEntity.ModeId,
            OrganizationId = new OrganizationId(approvalPolicyEntity.OrganizationId),
            RequireDeploymentApproval = new IsRequired(approvalPolicyEntity.RequireDeploymentApproval),
            CaseDecisionMinimumSeverity = approvalPolicyEntity.CaseDecisionMinimumSeverityId.HasValue
                ? (RuleSeverity)approvalPolicyEntity.CaseDecisionMinimumSeverityId.Value
                : null,
            CaseDecisionMatcher = approvalPolicyEntity.CaseDecisionMatcher.ToRuleBody(),
            CreatedAt = new CreatedAt(approvalPolicyEntity.CreatedAt),
            UpdatedAt = new UpdatedAt(approvalPolicyEntity.UpdatedAt)
        };
    }
}
