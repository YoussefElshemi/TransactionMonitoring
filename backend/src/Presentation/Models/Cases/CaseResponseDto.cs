using Presentation.Models.Alerts;
using Presentation.Models.CaseReasonCodes;
using Presentation.Models.RuleEvaluations;

namespace Presentation.Models.Cases;

public record CaseResponseDto
{
    public required Guid CaseId { get; init; }
    public required string FriendlyName { get; init; }
    public required string Status { get; init; }
    public required string Severity { get; init; }
    public required string? Disposition { get; init; }
    public required CaseReasonCodeResponseDto? ReasonCode { get; init; }
    public required Guid OrganizationId { get; init; }
    public required Guid? TransactionId { get; init; }
    public required Guid? PartyId { get; init; }
    public required RuleEvaluationResponseDto[] RuleEvaluations { get; init; }
    public required AlertResponseDto[] Alerts { get; init; }
    public required Guid? QueueId { get; init; }
    public required string? QueueName { get; init; }
    public required Guid? AssignedToUserId { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
    public required DateTimeOffset? AssignedAt { get; init; }
    public required DateTimeOffset DueAt { get; init; }
    public required DateTimeOffset? EscalatedAt { get; init; }
    public required Guid? EscalationRuleId { get; init; }
    public required DateTimeOffset? ResolvedAt { get; init; }
    public required DateTimeOffset? ReopenedAt { get; init; }

    public required string Mode { get; init; }
}
