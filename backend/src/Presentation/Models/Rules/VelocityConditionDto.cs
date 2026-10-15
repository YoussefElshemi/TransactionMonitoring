namespace Presentation.Models.Rules;

public record VelocityConditionDto
{
    public required string Role { get; init; }

    public required string Metric { get; init; }

    public required string Function { get; init; }

    public required VelocityWindowDto? Window { get; init; }

    public required bool? IncludeCurrent { get; init; }

    public string? Threshold { get; init; }

    public decimal? Band { get; init; }
}
