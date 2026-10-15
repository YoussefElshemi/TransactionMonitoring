namespace Presentation.Models.TransactionSchemas;

public record TransactionSchemaResponseDto
{
    public required Guid TransactionSchemaId { get; init; }
    public required Guid OrganizationId { get; init; }
    public required List<SchemaFieldDto> Fields { get; init; }
    public required int CurrentVersionNumber { get; init; }
    public required bool IsDeleted { get; init; }
    public required DateTimeOffset? DeletedAt { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }

    public required string Mode { get; init; }
}
