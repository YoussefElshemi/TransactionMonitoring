using Core.Enums;

namespace Core.Models.Rules;

public record VelocityConditionModel
{
    public PartyRole Role { get; init; }

    public string? Metric { get; init; }

    public VelocityFunction Function { get; init; }

    public VelocityWindowModel? Window { get; init; }

    public bool IncludeCurrent { get; init; }

    public string? Threshold { get; init; }

    public decimal? Band { get; init; }
}
