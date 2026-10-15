using Core.Enums;

namespace Core.Models.PartyMappings;

public record PartyRoleMappingModel
{
    public PartyRole Role { get; init; }

    public string? Path { get; init; }

    public string? IdentifierPath { get; init; }

    public string? DisplayNamePath { get; init; }

    public List<PartyAttributeMappingModel>? AttributePaths { get; init; }
}

public record PartyAttributeMappingModel
{
    public string? Name { get; init; }

    public string? Path { get; init; }
}
