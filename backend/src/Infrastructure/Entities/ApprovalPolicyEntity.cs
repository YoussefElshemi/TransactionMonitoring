using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record ApprovalPolicyEntity : BaseEntity, IModeScoped
{
    public required int ModeId { get; init; }

    public required Guid OrganizationId { get; init; }
    public OrganizationEntity Organization { get; init; } = null!;
    public required bool RequireDeploymentApproval { get; set; }
    public required int? CaseDecisionMinimumSeverityId { get; set; }
    public RuleSeverityEntity? CaseDecisionMinimumSeverityEntity { get; init; }

    public required string? CaseDecisionMatcher { get; set; }
}
