using Core.Enums;

namespace Core.Models.Rules;

public record PartyConditionModel
{
    public PartyRole Role { get; init; }

    public PartyAttribute Attribute { get; init; }
}
