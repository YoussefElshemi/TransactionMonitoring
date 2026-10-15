namespace Core.Models.Webhooks;

public record SuspiciousActivityReportWebhookMessage
{
    public required Guid SuspiciousActivityReportId { get; init; }
    public required Guid OrganizationId { get; init; }
    public required Guid CaseId { get; init; }
    public required string FriendlyName { get; init; }
    public required string Status { get; init; }
    public required DateTimeOffset DueAt { get; init; }
    public required UserWebhookMessage? OpenedByUser { get; init; }
    public required UserWebhookMessage? FiledByUser { get; init; }
    public required DateTimeOffset? FiledAt { get; init; }
    public required string? FilingReference { get; init; }
    public required DateTimeOffset? AcknowledgedAt { get; init; }
    public required string? AcknowledgementReference { get; init; }
    public required string? NotFiledReason { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
}
