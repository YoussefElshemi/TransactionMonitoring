namespace Infrastructure.Entities;

public record TransactionSchemaVersionEntity : IModeScoped
{
    public required int ModeId { get; init; }

    public required Guid TransactionSchemaVersionId { get; init; }
    public required Guid TransactionSchemaId { get; init; }
    public TransactionSchemaEntity TransactionSchema { get; init; } = null!;
    public required Guid OrganizationId { get; init; }
    public required int VersionNumber { get; init; }
    public required string Fields { get; init; }
    public required Guid CreatedByUserId { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}
