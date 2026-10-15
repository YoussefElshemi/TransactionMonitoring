using Core.Enums;
using Core.Models.BaseModels;
using Core.Models.Rules;
using Core.ValueObjects;

namespace Core.Models.Approvals;

public record ApprovalPolicy : BaseModel
{
    public required OrganizationId OrganizationId { get; init; }

    public required IsRequired RequireDeploymentApproval { get; init; }

    public required RuleSeverity? CaseDecisionMinimumSeverity { get; init; }

    public required RuleBody? CaseDecisionMatcher { get; init; }

    public required Mode Mode { get; init; }
}
