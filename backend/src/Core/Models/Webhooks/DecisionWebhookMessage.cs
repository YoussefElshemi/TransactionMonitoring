namespace Core.Models.Webhooks;

public record DecisionWebhookMessage
{
    public required Guid DecisionId { get; init; }
    public required Guid OrganizationId { get; init; }
    public required Guid TransactionId { get; init; }
    public required string? ExternalReference { get; init; }
    public required string Verdict { get; init; }
    public required string ReasonCode { get; init; }
    public required Guid? CaseId { get; init; }
    public required UserWebhookMessage? ResolvedByUser { get; init; }
    public required DecisionRuleWebhookMessage[] FiredRules { get; init; }
    public required DateTimeOffset? HeldAt { get; init; }
    public required DateTimeOffset? ExpiresAt { get; init; }
    public required string? HoldExpiryVerdict { get; init; }
    public required DateTimeOffset? ResolvedAt { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
}