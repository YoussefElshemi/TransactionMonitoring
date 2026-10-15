using Core.Enums;
using Core.Models.BaseModels;
using Core.Models.RuleEvaluations;
using Core.Models.Users;
using Core.ValueObjects;

namespace Core.Models.Decisions;

public record Decision : BaseModel
{
    public required DecisionId DecisionId { get; init; }
    public required OrganizationId OrganizationId { get; init; }
    public required TransactionId TransactionId { get; init; }
    public required ExternalReference? ExternalReference { get; init; }
    public required DecisionStatus Status { get; init; }
    public required DecisionReasonCode ReasonCode { get; init; }
    public required CaseId? CaseId { get; init; }
    public required List<RuleEvaluation> RuleEvaluations { get; init; }
    public required ApplicationUserId? ResolvedByUserId { get; init; }
    public required ApplicationUser? ResolvedByUser { get; init; }
    public required HeldAt? HeldAt { get; init; }
    public required ExpiresAt? ExpiresAt { get; init; }
    public required HoldExpiryVerdict? HoldExpiryVerdict { get; init; }
    public required ResolvedAt? ResolvedAt { get; init; }

    public required Mode Mode { get; init; }
}
