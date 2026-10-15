namespace Core.Models.Webhooks;

public record ApprovalWebhookMessage
{
    public required Guid ApprovalId { get; init; }
    public required Guid OrganizationId { get; init; }
    public required string Subject { get; init; }
    public required Guid? RuleDeploymentId { get; init; }
    public required Guid? CaseId { get; init; }
    public required Guid? SuspiciousActivityReportId { get; init; }
    public required string Status { get; init; }
    public required string? Reason { get; init; }
    public required UserWebhookMessage? RequestedByUser { get; init; }
    public required UserWebhookMessage? ReviewedByUser { get; init; }
    public required DateTimeOffset? ReviewedAt { get; init; }
    public required string? ReviewNote { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
}
