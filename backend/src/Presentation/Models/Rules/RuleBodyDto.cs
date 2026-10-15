namespace Presentation.Models.Rules;

public record RuleBodyDto
{
    public string? Condition { get; init; }

    public string? Field { get; init; }

    public string? Quantifier { get; init; }

    public RuleExpressionDto? Expression { get; init; }

    public PartyConditionDto? Party { get; init; }

    public VelocityConditionDto? Velocity { get; init; }

    public object? Value { get; init; }

    public RuleBodyDto[]? And { get; init; }

    public RuleBodyDto[]? Or { get; init; }

    public RuleBodyDto[]? Not { get; init; }
}
