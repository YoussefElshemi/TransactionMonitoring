using Core.Enums;
using Core.Models.TransactionSchemas;
using Core.ValueObjects;
using Presentation.Models.TransactionSchemas;

namespace Presentation.Mappers.TransactionSchemas;

public static class SchemaFieldDtoMapper
{
    public static List<SchemaFieldDto> ToDto(this List<SchemaField> fields)
    {
        return fields.ConvertAll(x => new SchemaFieldDto
        {
            Name = x.Name,
            Type = x.Type.ToString(),
            IsRequired = x.IsRequired,
            AllowedValues = x.AllowedValues,
            Fields = x.Fields?.ToDto(),
            ElementType = x.ElementType?.ToString()
        });
    }

    public static List<SchemaField> ToModel(this List<SchemaFieldDto> fields)
    {
        return fields.ConvertAll(x => new SchemaField
        {
            Name = new SchemaFieldName(x.Name),
            Type = Enum.Parse<SchemaFieldType>(x.Type, true),
            IsRequired = new IsRequired(x.IsRequired),
            AllowedValues = x.AllowedValues is { Count: > 0 } ? x.AllowedValues : null,
            Fields = x.Fields is { Count: > 0 } ? x.Fields.ToModel() : null,
            ElementType = !string.IsNullOrWhiteSpace(x.ElementType) ? Enum.Parse<SchemaFieldType>(x.ElementType, true) : null
        });
    }
}
