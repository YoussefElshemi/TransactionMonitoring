namespace Core.Models.Webhooks;

public record CaseWebhookMessage
{
    public required Guid CaseId { get; init; }
    public required string FriendlyName { get; init; }
    public required string Status { get; init; }
    public required string Severity { get; init; }
    public required string? Disposition { get; init; }
    public required string? ReasonCode { get; init; }
    public required Guid OrganizationId { get; init; }
    public required UserWebhookMessage? AssignedToUser { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
    public required DateTimeOffset? AssignedAt { get; init; }

    public required DateTimeOffset DueAt { get; init; }
    public required DateTimeOffset? EscalatedAt { get; init; }
    public required Guid? EscalationRuleId { get; init; }
    public required DateTimeOffset? ResolvedAt { get; init; }
    public required DateTimeOffset? ReopenedAt { get; init; }
}