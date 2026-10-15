namespace Presentation.Models.Alerts;

public record AlertResponseDto
{
    public required Guid AlertId { get; init; }
    public required string FriendlyName { get; init; }
    public required string Status { get; init; }
    public required string Severity { get; init; }
    public required Guid OrganizationId { get; init; }
    public required Guid RuleId { get; init; }
    public required Guid RuleVersionId { get; init; }
    public required Guid RuleEvaluationId { get; init; }
    public required Guid TransactionId { get; init; }
    public required Guid? PartyId { get; init; }
    public required string? PartyRole { get; init; }
    public required Guid? CaseId { get; init; }
    public required string? SuppressionReason { get; init; }
    public required Guid? SuppressedByAlertId { get; init; }
    public required DateTimeOffset RaisedAt { get; init; }
    public required DateTimeOffset? ClosedAt { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }

    public required string Mode { get; init; }
}
