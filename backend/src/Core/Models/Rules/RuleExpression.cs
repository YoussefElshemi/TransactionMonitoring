using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.Rules;

public record RuleExpression
{
    public RuleField? Field { get; init; }

    public RuleValue? Value { get; init; }

    public PartyCondition? Party { get; init; }

    public VelocityCondition? Velocity { get; init; }

    public RuleFunction? Function { get; init; }

    public List<RuleExpression>? Arguments { get; init; }
}
