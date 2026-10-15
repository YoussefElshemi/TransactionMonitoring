namespace Presentation.Models.TransactionSchemas;

public record TransactionSchemaVersionResponseDto
{
    public required Guid TransactionSchemaVersionId { get; init; }
    public required Guid TransactionSchemaId { get; init; }
    public required Guid OrganizationId { get; init; }
    public required int VersionNumber { get; init; }
    public required List<SchemaFieldDto> Fields { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }

    public required string Mode { get; init; }
}
