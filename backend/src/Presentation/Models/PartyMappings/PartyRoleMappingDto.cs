using System.Text.Json.Serialization;

namespace Presentation.Models.PartyMappings;

public record PartyRoleMappingDto
{
    public required string Role { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Path { get; init; }

    public required string IdentifierPath { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DisplayNamePath { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<PartyAttributeMappingDto>? AttributePaths { get; init; }
}

public record PartyAttributeMappingDto
{
    public required string Name { get; init; }
    public required string Path { get; init; }
}
