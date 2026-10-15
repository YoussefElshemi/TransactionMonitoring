using Core.Enums;
using Core.Models.BaseModels;
using Core.ValueObjects;

namespace Core.Models.TransactionSchemas;

public record TransactionSchema : DeletableModel
{
    public required TransactionSchemaId TransactionSchemaId { get; init; }
    public required OrganizationId OrganizationId { get; init; }
    public required List<SchemaField> Fields { get; init; }
    public required TransactionSchemaVersionId CurrentVersionId { get; init; }
    public required VersionNumber CurrentVersionNumber { get; init; }
    public required ApplicationUserId CreatedByUserId { get; init; }

    public required Mode Mode { get; init; }
}
