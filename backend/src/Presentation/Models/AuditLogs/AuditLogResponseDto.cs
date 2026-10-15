namespace Presentation.Models.AuditLogs;

public record AuditLogResponseDto
{
    public required Guid AuditLogId { get; init; }
    public required string EntityType { get; init; }
    public required Guid EntityId { get; init; }
    public required Guid? OrganizationId { get; init; }
    public required string Operation { get; init; }
    public required IReadOnlyList<AuditLogChangeDto> Changes { get; init; }
    public required Guid? ChangedByUserId { get; init; }
    public required Guid? ChangedByApiClientId { get; init; }

    public required long Sequence { get; init; }
    public required string? PreviousHash { get; init; }
    public required string Hash { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}
