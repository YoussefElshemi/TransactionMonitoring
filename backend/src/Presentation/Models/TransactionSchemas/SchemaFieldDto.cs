using System.Text.Json.Serialization;

namespace Presentation.Models.TransactionSchemas;

public record SchemaFieldDto
{
    public required string Name { get; init; }
    public required string Type { get; init; }
    public required bool IsRequired { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? AllowedValues { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<SchemaFieldDto>? Fields { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ElementType { get; init; }
}
