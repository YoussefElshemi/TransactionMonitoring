using Core.ValueObjects;

namespace Core.Models.Parties;

public record PartyAttributeValue
{
    public required PartyAttributeName Name { get; init; }
    public required PartyDisplayName Value { get; init; }
}
