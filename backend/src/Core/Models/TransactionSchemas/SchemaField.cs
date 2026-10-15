using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.TransactionSchemas;

public record SchemaField
{
    public required SchemaFieldName Name { get; init; }
    public required SchemaFieldType Type { get; init; }
    public required IsRequired IsRequired { get; init; }

    public required List<string>? AllowedValues { get; init; }

    public List<SchemaField>? Fields { get; init; }

    public SchemaFieldType? ElementType { get; init; }
}
