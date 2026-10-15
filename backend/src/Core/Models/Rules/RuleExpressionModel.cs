using Core.Enums;

namespace Core.Models.Rules;

public record RuleExpressionModel
{
    public string? Field { get; init; }

    public object? Value { get; init; }

    public PartyConditionModel? Party { get; init; }

    public VelocityConditionModel? Velocity { get; init; }

    public RuleFunction? Function { get; init; }

    public RuleExpressionModel[]? Arguments { get; init; }
}
