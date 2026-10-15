using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.TransactionSchemas;

public record TransactionSchemaVersion
{
    public required TransactionSchemaVersionId TransactionSchemaVersionId { get; init; }
    public required TransactionSchemaId TransactionSchemaId { get; init; }
    public required OrganizationId OrganizationId { get; init; }
    public required VersionNumber VersionNumber { get; init; }
    public required List<SchemaField> Fields { get; init; }
    public required ApplicationUserId CreatedByUserId { get; init; }
    public required CreatedAt CreatedAt { get; init; }

    public required Mode Mode { get; init; }
}
