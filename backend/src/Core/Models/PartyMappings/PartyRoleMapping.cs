using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.PartyMappings;

public record PartyRoleMapping
{
    public required PartyRole Role { get; init; }

    public required PartyPath? Path { get; init; }

    public required PartyPath IdentifierPath { get; init; }

    public required PartyPath? DisplayNamePath { get; init; }

    public List<PartyAttributeMapping>? AttributePaths { get; init; }
}
