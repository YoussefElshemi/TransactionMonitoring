namespace Infrastructure.Entities;

public record AuditAnchorEntity
{
    public required Guid AuditAnchorId { get; init; }
    public required Guid? OrganizationId { get; init; }
    public required long Sequence { get; init; }
    public required string Hash { get; init; }
    public required string Signature { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}
