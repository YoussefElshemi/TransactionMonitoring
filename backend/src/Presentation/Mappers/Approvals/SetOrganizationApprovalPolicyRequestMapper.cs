using Core.Enums;
using Core.Models.Approvals;
using Core.ValueObjects;
using Presentation.Mappers.Rules;
using Presentation.Models.Approvals;

namespace Presentation.Mappers.Approvals;

public static class SetOrganizationApprovalPolicyRequestMapper
{
    public static SetApprovalPolicyRequest ToModel(this SetOrganizationApprovalPolicyRequestDto setOrganizationApprovalPolicyRequestDto)
    {
        return new SetApprovalPolicyRequest
        {
            OrganizationId = new OrganizationId(setOrganizationApprovalPolicyRequestDto.OrganizationId),
            RequireDeploymentApproval = new IsRequired(setOrganizationApprovalPolicyRequestDto.Body.RequireDeploymentApproval),
            CaseDecisionMinimumSeverity = !string.IsNullOrWhiteSpace(setOrganizationApprovalPolicyRequestDto.Body.CaseDecisionMinimumSeverity)
                ? Enum.Parse<RuleSeverity>(setOrganizationApprovalPolicyRequestDto.Body.CaseDecisionMinimumSeverity, true)
                : null,
            CaseDecisionMatcher = setOrganizationApprovalPolicyRequestDto.Body.CaseDecisionMatcher?.ToModel()
        };
    }
}
