using Core.Enums;

namespace Core.Models.Rules;

public record PartyCondition
{
    public required PartyRole Role { get; init; }
    public required PartyAttribute Attribute { get; init; }
}
