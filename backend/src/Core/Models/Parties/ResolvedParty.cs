using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.Parties;

public record ResolvedParty
{
    public required PartyRole Role { get; init; }
    public required PartyIdentifier Identifier { get; init; }
    public required PartyDisplayName? DisplayName { get; init; }
    public required List<PartyAttributeValue> Attributes { get; init; }
}
