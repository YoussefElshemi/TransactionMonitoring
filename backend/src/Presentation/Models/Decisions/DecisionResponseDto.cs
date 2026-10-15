using Presentation.Models.RuleEvaluations;

namespace Presentation.Models.Decisions;

public record DecisionResponseDto
{
    public required Guid DecisionId { get; init; }
    public required Guid TransactionId { get; init; }
    public required string? TransactionReference { get; init; }
    public required string Status { get; init; }
    public required string ReasonCode { get; init; }
    public required Guid? CaseId { get; init; }
    public required Guid? ResolvedByUserId { get; init; }
    public required RuleEvaluationResponseDto[] FiredRules { get; init; }
    public required DateTimeOffset? HeldAt { get; init; }
    public required DateTimeOffset? ExpiresAt { get; init; }
    public required string? HoldExpiryVerdict { get; init; }
    public required DateTimeOffset? ResolvedAt { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }

    public required string Mode { get; init; }
}
