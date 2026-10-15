namespace Presentation.Models.Rules;

public record RuleExpressionDto
{
    public string? Field { get; init; }

    public object? Value { get; init; }

    public PartyConditionDto? Party { get; init; }

    public VelocityConditionDto? Velocity { get; init; }

    public string? Function { get; init; }

    public RuleExpressionDto[]? Arguments { get; init; }
}
