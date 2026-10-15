namespace Infrastructure.Entities;

public record AuditLogEntity
{
    public required Guid AuditLogId { get; init; }
    public required int EntityTypeId { get; init; }
    public AuditableEntityTypeEntity EntityTypeEntity { get; init; } = null!;
    public required Guid EntityId { get; init; }
    public required Guid? OrganizationId { get; init; }
    public required int OperationId { get; init; }
    public AuditOperationEntity OperationEntity { get; init; } = null!;
    public required string Snapshot { get; init; }
    public required Guid? ChangedByUserId { get; init; }
    public required Guid? ChangedByApiClientId { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }

    // Written by the insert trigger, never by this process.
    public long Sequence { get; init; }
    public string? PreviousHash { get; init; }
    public string Hash { get; init; } = null!;
}
