using Core.ValueObjects;

namespace Core.Models.TransactionSchemas;

public record SetTransactionSchemaRequest
{
    public required OrganizationId OrganizationId { get; init; }
    public required List<SchemaField> Fields { get; init; }
    public required ApplicationUserId CreatedByUserId { get; init; }
}
