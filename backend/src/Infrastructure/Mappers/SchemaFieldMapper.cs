using Core.Extensions;
using Core.Models.TransactionSchemas;
using Core.ValueObjects;

namespace Infrastructure.Mappers;

public static class SchemaFieldMapper
{
    public static List<SchemaField> ToModel(this List<SchemaFieldModel> fields)
    {
        return fields.ConvertAll(x => new SchemaField
        {
            Name = new SchemaFieldName(x.Name ?? string.Empty),
            Type = x.Type,
            IsRequired = new IsRequired(x.IsRequired),
            AllowedValues = x.AllowedValues,
            Fields = x.Fields?.ToModel(),
            ElementType = x.ElementType
        });
    }

    public static List<SchemaField> FieldsToModel(this string fields)
    {
        return fields.FromBase64String().TryDeserialize<List<SchemaFieldModel>>().ToModel();
    }

    public static string ToBase64String(this List<SchemaField> fields)
    {
        return fields.ToEntity().Serialize().ToBase64String();
    }

    private static List<SchemaFieldModel> ToEntity(this List<SchemaField> fields)
    {
        return fields.ConvertAll(x => new SchemaFieldModel
        {
            Name = x.Name,
            Type = x.Type,
            IsRequired = x.IsRequired,
            AllowedValues = x.AllowedValues,
            Fields = x.Fields?.ToEntity(),
            ElementType = x.ElementType
        });
    }
}
