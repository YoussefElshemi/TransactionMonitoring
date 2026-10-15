namespace Presentation.Models.Rules;

public record PartyConditionDto
{
    public required string Role { get; init; }

    public required string Attribute { get; init; }
}
