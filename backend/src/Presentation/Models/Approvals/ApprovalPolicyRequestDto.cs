using Microsoft.AspNetCore.Mvc;
using Presentation.Models.Rules;

namespace Presentation.Models.Approvals;

public record OrganizationApprovalPolicyRequestDto
{
    [FromRoute]
    public required Guid OrganizationId { get; init; }
}

public record SetOrganizationApprovalPolicyRequestDto : OrganizationApprovalPolicyRequestDto
{
    [FromBody]
    public required SetOrganizationApprovalPolicyRequestBodyDto Body { get; init; }
}

public record SetOrganizationApprovalPolicyRequestBodyDto
{
    public required bool RequireDeploymentApproval { get; init; }

    public string? CaseDecisionMinimumSeverity { get; init; }

    public RuleBodyDto? CaseDecisionMatcher { get; init; }
}

public record ApprovalPolicyResponseDto
{
    public required Guid OrganizationId { get; init; }
    public required bool RequireDeploymentApproval { get; init; }
    public required string? CaseDecisionMinimumSeverity { get; init; }
    public required RuleBodyDto? CaseDecisionMatcher { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }

    public required string Mode { get; init; }
}
