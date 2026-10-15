using Core.Enums;

namespace Core.Models.Rules;

public record RuleBodyModel
{
    public RuleCondition? Condition { get; init; }

    public string? Field { get; init; }

    public RuleQuantifier? Quantifier { get; init; }

    public RuleExpressionModel? Expression { get; init; }

    public PartyConditionModel? Party { get; init; }

    public VelocityConditionModel? Velocity { get; init; }

    public object? Value { get; init; }

    public RuleBodyModel[]? And { get; init; }

    public RuleBodyModel[]? Or { get; init; }

    public RuleBodyModel[]? Not { get; init; }
}
