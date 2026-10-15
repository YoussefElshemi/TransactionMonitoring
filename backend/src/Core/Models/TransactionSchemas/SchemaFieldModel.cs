using Core.Enums;

namespace Core.Models.TransactionSchemas;

public record SchemaFieldModel
{
    public string? Name { get; init; }

    public SchemaFieldType Type { get; init; }

    public bool IsRequired { get; init; }

    public List<string>? AllowedValues { get; init; }

    public List<SchemaFieldModel>? Fields { get; init; }

    public SchemaFieldType? ElementType { get; init; }
}
