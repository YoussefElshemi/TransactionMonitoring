using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.Rules;

public record RuleBody
{
    public required RuleCondition? Condition { get; init; }

    public required RuleField? Field { get; init; }

    public RuleQuantifier? Quantifier { get; init; }

    public RuleExpression? Expression { get; init; }

    public PartyCondition? Party { get; init; }

    public VelocityCondition? Velocity { get; init; }

    public required RuleValue? Value { get; init; }

    public required List<RuleBody>? And { get; init; }

    public required List<RuleBody>? Or { get; init; }

    public required List<RuleBody>? Not { get; init; }
}
