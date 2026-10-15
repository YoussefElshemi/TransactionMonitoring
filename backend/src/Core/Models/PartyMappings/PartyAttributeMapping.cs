using Core.ValueObjects;

namespace Core.Models.PartyMappings;

public record PartyAttributeMapping
{
    public required PartyAttributeName Name { get; init; }
    public required PartyPath Path { get; init; }
}
