using Core.Enums;
using Core.Models.Alerts;
using Core.Models.BaseModels;
using Core.Models.CaseReasonCodes;
using Core.Models.Queues;
using Core.Models.RuleEvaluations;
using Core.Models.Users;
using Core.ValueObjects;

namespace Core.Models.Cases;

public record Case : DeletableModel
{
    public required CaseId CaseId { get; init; }
    public required FriendlyName FriendlyName { get; init; }
    public required CaseStatus Status { get; init; }
    public required RuleSeverity Severity { get; init; }
    public required OrganizationId OrganizationId { get; init; }
    public required TransactionId? TransactionId { get; init; }

    public required PartyId? PartyId { get; init; }
    public required List<RuleEvaluation> RuleEvaluations { get; init; }
    public required List<Alert> Alerts { get; init; }

    public required CaseDisposition? Disposition { get; init; }
    public required CaseReasonCodeId? CaseReasonCodeId { get; init; }
    public required CaseReasonCode? ReasonCode { get; init; }

    public required QueueId? QueueId { get; init; }
    public required Queue? Queue { get; init; }
    public required ApplicationUserId? AssignedToUserId { get; init; }
    public required ApplicationUser? AssignedToUser { get; init; }
    public required AssignedAt? AssignedAt { get; init; }

    public required DueAt DueAt { get; init; }

    public required EscalatedAt? EscalatedAt { get; init; }
    public required EscalationRuleId? EscalationRuleId { get; init; }
    public required BreachNotifiedAt? BreachNotifiedAt { get; init; }
    public required ResolvedAt? ResolvedAt { get; init; }
    public required ReopenedAt? ReopenedAt { get; init; }

    public required Mode Mode { get; init; }
}