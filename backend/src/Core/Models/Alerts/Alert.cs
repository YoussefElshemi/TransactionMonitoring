using Core.Enums;
using Core.Models.BaseModels;
using Core.Models.RuleEvaluations;
using Core.ValueObjects;

namespace Core.Models.Alerts;

public record Alert : DeletableModel
{
    public required AlertId AlertId { get; init; }
    public required FriendlyName FriendlyName { get; init; }
    public required OrganizationId OrganizationId { get; init; }
    public required AlertStatus Status { get; init; }
    public required RuleSeverity Severity { get; init; }
    public required RuleId RuleId { get; init; }
    public required RuleVersionId RuleVersionId { get; init; }
    public required RuleEvaluationId RuleEvaluationId { get; init; }
    public required RuleEvaluation? RuleEvaluation { get; init; }
    public required TransactionId TransactionId { get; init; }

    public required PartyId? PartyId { get; init; }
    public required PartyRole? PartyRole { get; init; }

    public required CaseId? CaseId { get; init; }

    public required AlertSuppressionReason? SuppressionReason { get; init; }
    public required AlertId? SuppressedByAlertId { get; init; }

    // Separate from CreatedAt: the dedup window anchors on it, so replays and backfills don't read as a burst.
    public required RaisedAt RaisedAt { get; init; }
    public required ClosedAt? ClosedAt { get; init; }

    public required Mode Mode { get; init; }
}
